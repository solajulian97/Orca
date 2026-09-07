"""Verify the settings-only edit against the user-requested pre-change ZIP."""
from pathlib import Path
import re
import zipfile

root = Path(__file__).resolve().parents[2]
rel = 'Orca Trades/Working_Suite/Indicators/OrcaCandleVolumeProfile.cs'
with zipfile.ZipFile(root / '.codex-backups/cvp-pre-settings-2026-09-07_000755/CVP-pre-settings.zip') as archive:
    before = archive.read('workspace/' + rel).decode('utf-8-sig').replace('\r\n', '\n')
    for dependency in ['OrcaCandleVolumeProfile.Rendering.cs', 'OrcaFootprintCore.cs']:
        name = 'Orca Trades/Working_Suite/Indicators/' + dependency
        assert (root / name).read_bytes() == archive.read('workspace/' + name), dependency
after = (root / rel).read_text(encoding='utf-8-sig')

def indicator_contract(text):
    text = text.split('    // Keep the IndicatorBaseConverter')[0]
    text = re.sub(r'^\s*\[Display\([^\n]*\)\]\s*$', '', text, flags=re.M)
    return re.sub(r'Description = "[^"\n]*";', '', text)

assert indicator_contract(before) == indicator_contract(after), 'Non-presentation indicator change'
assert before.split('#region NinjaScript generated code.')[1] == after.split('#region NinjaScript generated code.')[1], 'Generated wrappers changed'
pattern = r'\[Display\(([^\n]*)\)\]\s*public\s+[\w.<>]+\s+(\w+)'
old_settings = {name for _, name in re.findall(pattern, before)}
settings = re.findall(pattern, after)
assert old_settings == {name for _, name in settings}, 'Setting removed or added'
positions = set()
for attributes, name in settings:
    group = re.search(r'GroupName = "([^"]*)"', attributes)[1]
    order = int(re.search(r'Order = (\d+)', attributes)[1])
    assert (group, order) not in positions, (name, group, order)
    positions.add((group, order))
assert len({group for group, _ in positions}) == 11
print(f'PASS: {len(settings)} settings retained, 11 groups, no duplicate positions; all indicator logic, defaults, property identities, serialization, wrappers and both dependencies match backup.')
