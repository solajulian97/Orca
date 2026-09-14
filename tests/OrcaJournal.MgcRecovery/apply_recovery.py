"""Apply the six reviewed MGC rows additively; never replace a Journal database.

Usage: python apply_recovery.py reviewed-copy.db target.db
The deployment wrapper is responsible for checking NinjaTrader is closed and
backing up production. This tool also checks NinjaTrader before any transaction.
"""
import json
import pathlib
import sqlite3
import subprocess
import sys

source, target = map(pathlib.Path, sys.argv[1:3])
copy_test = len(sys.argv) == 4 and sys.argv[3] == '--copy-test'
if copy_test:
    assert target.name.startswith('recovery-test-') and target.resolve().parent == source.resolve().parent
if not copy_test:
    running = subprocess.run(['powershell.exe', '-NoProfile', '-Command', 'if(Get-Process NinjaTrader -ErrorAction SilentlyContinue){exit 2}'], capture_output=True, text=True)
    if running.returncode != 0:
        raise SystemExit('Close NinjaTrader before applying recovery (or process check failed).')
with sqlite3.connect(source.as_uri()+'?mode=ro', uri=True) as candidate:
    candidate.row_factory = sqlite3.Row
    rows = [dict(r) for r in candidate.execute("SELECT * FROM trades WHERE account='TAKEPROFIT262460251' AND instrument='MGC' AND session_date='2026-09-14'")]
    assert len(rows) == 6 and abs(sum(r['pnl_dollars'] for r in rows)+60) < .001
    identities = {r['trade_id']: dict(r) for r in candidate.execute('SELECT * FROM trade_identity')}
with sqlite3.connect(target.as_uri()+'?mode=rw', uri=True) as db:
    db.row_factory = sqlite3.Row
    db.execute('BEGIN IMMEDIATE')
    before = {name: [tuple(r) for r in db.execute('SELECT * FROM "'+name+'" ORDER BY rowid')]
              for (name,) in db.execute("SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'")}
    added = 0
    for row in rows:
        identity = identities[row['id']]
        p = json.loads(identity['provenance_json'])
        assert not p['CompleteHistory'] and row['mae'] is None and row['mfe'] is None
        existing = db.execute('SELECT * FROM trades WHERE trade_key=?', (row['trade_key'],)).fetchone()
        if existing:
            assert all(existing[k] == v for k, v in row.items() if k not in ('id', 'notes', 'setup_grade'))
            continue
        ids = {a['ExecutionId'] for a in p['Allocations']}
        for (payload,) in db.execute('SELECT i.provenance_json FROM trade_identity i JOIN trades t ON t.id=i.trade_id WHERE t.account=? AND t.instrument_full_name=?', (row['account'], row['instrument_full_name'])):
            assert not ids.intersection(a['ExecutionId'] for a in json.loads(payload or '{}').get('Allocations', [])), 'Overlapping execution evidence'
        values = {k:v for k,v in row.items() if k != 'id'}
        inserted = db.execute('INSERT INTO trades ('+','.join(values)+') VALUES ('+','.join('?' for _ in values)+')', list(values.values())).lastrowid
        identity['trade_id'] = inserted
        identity['trade_uid'] = None
        db.execute('INSERT INTO trade_identity ('+','.join(identity)+') VALUES ('+','.join('?' for _ in identity)+')', list(identity.values()))
        added += 1
    for table, original in before.items():
        after = [tuple(r) for r in db.execute('SELECT * FROM "'+table+'" ORDER BY rowid')]
        if table in ('trades', 'trade_identity'):
            assert after[:len(original)] == original and len(after) == len(original)+added
        else:
            assert after == original, 'Unexpected changes to '+table
    assert db.execute('PRAGMA integrity_check').fetchone()[0] == 'ok'
    db.commit()
print('Added', added, 'reviewed MGC rows; existing history and media preserved.')
