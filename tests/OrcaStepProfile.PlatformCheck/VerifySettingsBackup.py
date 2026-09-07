"""Verify the metadata-only Step Profile cleanup against its pre-change ZIP.

Run from any directory with Python 3. Optional --live also checks deployed authored
parity. This is source verification, not NinjaTrader F5 or template validation.
"""
from pathlib import Path
import re
import sys
import zipfile

root = Path(__file__).resolve().parents[2]
relative = 'Orca Trades/Working_Suite/Indicators/OrcaStepProfile.cs'
backup = root / '.codex-backups/step-profile-pre-settings-2026-09-07_131040/StepProfile-pre-settings.zip'
with zipfile.ZipFile(backup) as archive:
    before = archive.read('workspace/' + relative).decode('utf-8-sig').replace('\r\n', '\n')
after = (root / relative).read_text(encoding='utf-8-sig')
old_description = 'Time-, volume-, or session-based step profiles with dual volume/delta histograms, gradient, POC, and Value Area.'
new_description = 'Displays volume and delta profiles by time interval, traded volume, or market session. Includes active and historical profiles, point of control, value area, delta labels, and profile statistics.'
assert after.count('Description = "' + new_description + '";') == 1

def without_display_metadata(text):
    return re.sub(r'\[Display\([^\n]*\)\]', '[Display()]', text)

assert without_display_metadata(before.replace(old_description, new_description)) == without_display_metadata(after), \
    'Change outside Display metadata and the approved description'
pattern = r'\[Display\(([^\n]*)\)\]\s*public\s+[\w.<>]+\s+(\w+)'
original = {name for _, name in re.findall(pattern, before)}
settings = {name: attributes for attributes, name in re.findall(pattern, after)}
assert original == set(settings), 'Setting removed, added or renamed'
assert len(settings) == 88
positions = set()
for name, attributes in settings.items():
    group = re.search(r'GroupName = "([^"]*)"', attributes)[1]
    order = int(re.search(r'Order = (\d+)', attributes)[1])
    assert (group, order) not in positions, (name, group, order)
    positions.add((group, order))
assert len({group for group, _ in positions}) == 11

cvp = (root / 'Orca Trades/Working_Suite/Indicators/OrcaCandleVolumeProfile.cs').read_text(encoding='utf-8-sig')
cvp_settings = {name: attributes for attributes, name in re.findall(pattern, cvp)}
shared = ['ShowPOC', 'POCBrush', 'ShowValueArea', 'ValueAreaPercent', 'ShowVAColor', 'VABrush',
          'ShowVALines', 'VALineBrush', 'VALineThickness', 'VALineStyle', 'MinBrightness',
          'ProfileBarSpacingPx', 'DeltaTextMinThreshold', 'DeltaTextFontSize']
for name in shared:
    label = lambda attributes: re.search(r'^Name = "([^"]*)"', attributes)[1]
    assert label(settings[name]) == label(cvp_settings[name]), 'CVP label mismatch: ' + name
print('PASS: 88 settings retained in 11 groups, unique ordering, 14 shared CVP labels match.')
print('PASS: all code outside Display metadata and the exact description matches backup, including defaults, properties, serialization, converters, rendering, calculations and generated wrappers.')
if '--live' in sys.argv:
    live = Path.home() / 'Documents/NinjaTrader 8/bin/Custom/Indicators/OrcaStepProfile.cs'
    authored = lambda text: text.split('#region NinjaScript generated code')[0].rstrip()
    assert authored(after) == authored(live.read_text(encoding='utf-8-sig')), 'Deployed source differs'
    print('PASS: deployed authored Step Profile matches Working_Suite.')
