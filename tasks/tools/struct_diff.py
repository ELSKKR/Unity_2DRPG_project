import json, sys
sys.stdout.reconfigure(encoding='utf-8')
D = r'C:\Users\User\AppData\Local\Temp\claude\D--Unity-MyJRPG\2ccd218b-cd9a-493d-9afd-111502726170\scratchpad\\'
RENAME = {'村口那位先生是誰？': '南邊屋子門口坐著的那位先生是誰？',
          '南邊屋子門口坐著的那位先生是誰？': '南邊屋子外面坐著的那位先生是誰？',
          '他說那點傷死不了。還有，要道歉就自己去，他就坐在門口等你。': '他說那點傷死不了。還有，要道歉就自己去，他就坐在屋外等你。'}
TEXT = {'lines', 'label', 'reactionLines', 'choices'}
SEGS = ['intro', 'reminder', 'handIn', 'completed']
STRUCT_CHOICE = ['acceptsQuest', 'completesHandIn', 'markEventID', 'requiredIntel', 'grantsIntel']

total_diffs = 0
for n in sys.argv[1:]:
    b = json.load(open(D + 'before_' + n + '.json', encoding='utf-8'))['MonoBehaviour']
    a = json.load(open(D + 'after_' + n + '.json', encoding='utf-8'))['MonoBehaviour']
    diffs, checked, new_choices = [], 0, []
    if len(b['stages']) != len(a['stages']):
        diffs.append('stage 數量不同')
    for i, (sb, sa) in enumerate(zip(b['stages'], a['stages'])):
        for k in sb:
            if k in SEGS:
                continue
            checked += 1
            if sb[k] != sa.get(k):
                diffs.append(f'stage{i}.{k}: {sb[k]} → {sa.get(k)}')
        for seg in SEGS:
            for k in sb[seg]:
                if k in TEXT:
                    continue
                checked += 1
                if sb[seg][k] != sa[seg].get(k):
                    diffs.append(f'stage{i}.{seg}.{k}')
            after_by_label = {c['label']: c for c in sa[seg]['choices']}
            old_labels = set()
            for cb in sb[seg]['choices']:
                lbl = RENAME.get(cb['label'], cb['label'])
                old_labels.add(lbl)
                ca = after_by_label.get(lbl)
                if ca is None:
                    diffs.append(f'stage{i}.{seg} 少了既有選項「{cb["label"]}」')
                    continue
                for k in STRUCT_CHOICE:
                    checked += 1
                    if cb[k] != ca[k]:
                        diffs.append(f'stage{i}.{seg}「{lbl}」.{k}: {cb[k]} → {ca[k]}')
                if cb.get('hideIfEventID', '') != ca.get('hideIfEventID', ''):
                    new_choices.append(f'stage{i}.{seg}「{lbl}」hideIfEventID: "{cb.get("hideIfEventID", "")}" → "{ca.get("hideIfEventID", "")}"（刻意新增）')
            for ca in sa[seg]['choices']:
                if ca['label'] not in old_labels:
                    new_choices.append(f'stage{i}.{seg}「{ca["label"]}」accepts={ca["acceptsQuest"]} completes={ca["completesHandIn"]} mark="{ca["markEventID"]}" grants={ca["grantsIntel"].get("guid", "")[:6] or "無"}')
                    if ca['acceptsQuest'] or ca['completesHandIn']:
                        diffs.append(f'新選項「{ca["label"]}」勾了 accept/complete')
    for k in b:
        if k not in ('stages', 'chats') and b[k] != a.get(k) and k not in ('m_Name',):
            if k == 'speakerName':
                diffs.append('speakerName 變了')
    total_diffs += len(diffs)
    print(f'== {n}: 比對結構欄位 {checked} 個，差異 {len(diffs)} 筆')
    for d in diffs:
        print('   ✗', d)
    for c in new_choices:
        print('   + 新選項', c)
print('總差異', total_diffs)
