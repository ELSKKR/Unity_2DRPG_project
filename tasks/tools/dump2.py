import re, sys, os, glob

os.chdir(r'D:\Unity\MyJRPG')
pat = re.compile(r'(?:\\u[0-9a-fA-F]{4})+')


def dec(s):
    return pat.sub(lambda m: m.group(0).encode().decode('unicode_escape'), s)


guid = {}
for m in glob.glob('Assets/**/*.asset.meta', recursive=True):
    g = re.search(r'guid: (\w+)', open(m, encoding='utf-8').read()).group(1)
    guid[g] = os.path.basename(m)[:-11]

skip = re.compile(r'm_Script|ObjectHideFlags|Corresponding|PrefabInstance|PrefabAsset|m_GameObject|'
                  r'm_Enabled|EditorHideFlags|YAML|%TAG|MonoBehaviour|--- |typingSound|startSound')
out = []
for f in sys.argv[1:]:
    out.append('=== ' + f)
    t = open(f, encoding='utf-8').read()
    t = re.sub(r'\n\s+(?=[^\s-][^:]*$)', ' ', t, flags=re.M)  # 接回 YAML 折行
    for line in t.splitlines():
        if skip.search(line):
            continue
        line = dec(line)
        line = re.sub(r'\{fileID: 11400000, guid: (\w+), type: 2\}',
                      lambda m: '<' + guid.get(m.group(1), m.group(1)) + '>', line)
        out.append(line)

dest = r'C:\Users\User\AppData\Local\Temp\claude\D--Unity-MyJRPG\2ccd218b-cd9a-493d-9afd-111502726170\scratchpad\dump2.txt'
open(dest, 'w', encoding='utf-8').write('\n'.join(out))
print('ok', len(out))
