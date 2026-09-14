#!/usr/bin/env python3
"""把 UI 會顯示的文字，比對字型資產裡「已經烘進去」的字元表。

背景（見 Docs/系統開發歷程.md 系統 9）：專案用 Silver 當主字型、Cubic 當備援，
兩個字型的 ClearDynamicDataOnBuild 都關掉了，字元是事先烘進資產的。
只要有人寫了一個沒烘過的字，編輯器裡看起來正常，打包版卻會缺字或高低不齊——
而且要等到打包完才會發現。這支腳本就是把那個回饋拉到「改完馬上知道」。

用法：
    python3 Tools/check_baked_chars.py          # 檢查全部，有缺字時 exit 1
    python3 Tools/check_baked_chars.py -v       # 連同每個缺字出現在哪裡一起印

新增了沒烘過的字時，要重跑一次「烘字 + FontBaselineAligner 對齊」流程，
否則打包版那些字會壞掉。
"""
import os
import re
import sys
from collections import defaultdict

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
FONT_ASSETS = [
    'Assets/TextMesh Pro/Fonts/Silver SDF.asset',
    'Assets/Fonts/Cubic SDF.asset',
]
# 這些副檔名裡的文字最後會走到 TextMeshPro 上
SCENE_EXT = ('.unity', '.prefab')
DATA_EXT = ('.asset',)

# 不算「缺字」的：ASCII 由字型本身覆蓋，控制字元和 TMP 的標記語法不會被畫出來
def is_exempt(ch):
    return ord(ch) < 0x80 or ch in '　﻿'


def read(path):
    with open(path, encoding='utf8', errors='replace') as fh:
        return fh.read()


def baked_charset():
    """字型資產裡 m_Unicode 的聯集，就是實際烘進去的字。"""
    chars = set()
    missing = []
    for rel in FONT_ASSETS:
        path = os.path.join(ROOT, rel)
        if not os.path.exists(path):
            missing.append(rel)
            continue
        chars |= {int(u) for u in re.findall(r'm_Unicode: (\d+)', read(path))}
    return {chr(c) for c in chars}, missing


def scene_strings(path):
    """場景/prefab 裡 TextMeshPro 元件的 m_text。"""
    for m in re.finditer(r'^\s*m_text:\s*(.*)$', read(path), re.M):
        yield unquote_yaml(m.group(1))


def data_strings(path):
    """ScriptableObject（對話、任務、道具、情報）裡的文字欄位。"""
    for m in re.finditer(r'^\s*\w+:\s*(.*[⺀-鿿＀-￯].*)$', read(path), re.M):
        yield unquote_yaml(m.group(1))


def script_strings(path):
    """.cs 裡「真的會被畫到畫面上」的字串字面值。

    要排除的是那些永遠不會經過 TextMeshPro 的字串——註解、Inspector 的
    [Header]/[Tooltip] 標註、Debug 訊息、編輯器選單——它們用什麼字都無所謂，
    留著只會製造假警報，讓人開始忽略這支腳本的輸出。
    """
    src = read(path)
    src = re.sub(r'/\*.*?\*/', ' ', src, flags=re.S)   # 區塊註解
    src = re.sub(r'//.*', ' ', src)                      # 行註解
    src = re.sub(r'\[\s*(?:\w+\.)*(?:Header|Tooltip|Space|Range|SerializeField|TextArea|ContextMenu|'
                 r'MenuItem|HelpURL|CreateAssetMenu|AddComponentMenu)\b.*?\]', ' ', src, flags=re.S)
    src = re.sub(r'\b(?:Debug\.\w+|EditorUtility\.\w+|EditorGUILayout\.\w+|'
                 r'EditorApplication\.\w+|Assert\.\w+)\s*\((?:[^()"]|"(?:[^"\\]|\\.)*")*\)',
                 ' ', src)                               # 只給開發者看的訊息
    for m in re.finditer(r'"((?:[^"\\\n]|\\.)*)"', src):
        s = m.group(1)
        if any(not is_exempt(c) for c in s):
            yield re.sub(r'\\u([0-9a-fA-F]{4})', lambda mm: chr(int(mm.group(1), 16)), s)


def unquote_yaml(v):
    v = v.strip()
    if len(v) >= 2 and v[0] == v[-1] and v[0] in '\'"':
        v = v[1:-1]
    # Unity 把非 ASCII 寫成 \uXXXX
    return re.sub(r'\\u([0-9a-fA-F]{4})', lambda m: chr(int(m.group(1), 16)), v)


def walk():
    for base, dirs, files in os.walk(os.path.join(ROOT, 'Assets')):
        dirs[:] = [d for d in dirs if d not in ('_Recovery',)]
        if 'TextMesh Pro' + os.sep + 'Examples' in base:
            continue   # TMP 附的範例場景不是我們的內容
        for f in files:
            path = os.path.join(base, f)
            rel = os.path.relpath(path, ROOT)
            if f.endswith(SCENE_EXT):
                yield rel, scene_strings(path)
            elif f.endswith(DATA_EXT) and 'TextMesh Pro' not in rel and 'Fonts' not in rel:
                yield rel, data_strings(path)
            elif f.endswith('.cs'):
                yield rel, script_strings(path)


def main():
    verbose = '-v' in sys.argv
    baked, missing_assets = baked_charset()
    if missing_assets:
        print('找不到字型資產：' + '、'.join(missing_assets), file=sys.stderr)
        return 2

    unbaked = defaultdict(set)   # 字元 -> 出現的檔案
    scanned = 0
    for rel, strings in walk():
        scanned += 1
        for s in strings:
            for ch in s:
                if not is_exempt(ch) and ch not in baked:
                    unbaked[ch].add(rel)

    print(f'已烘字元 {len(baked)} 個，掃描 {scanned} 個檔案。')
    if not unbaked:
        print('沒有缺字，打包版的文字不會壞掉。')
        return 0

    print(f'\n發現 {len(unbaked)} 個沒烘進字型的字元：')
    print('  ' + ''.join(sorted(unbaked)))
    if verbose:
        for ch in sorted(unbaked):
            print(f'\n  {ch}  U+{ord(ch):04X}')
            for rel in sorted(unbaked[ch]):
                print(f'      {rel}')
    print('\n這些字在編輯器裡看起來可能正常，但打包後會缺字或高低不齊。')
    print('請重跑一次「烘字 + FontBaselineAligner 對齊」流程，或把文案改成已烘過的字。')
    return 1


if __name__ == '__main__':
    sys.exit(main())
