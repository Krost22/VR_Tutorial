// usage: node grab.mjs <windowType> <out.png> [w h] [scroll]   (scroll: -1 keep, 100000 = bottom)
// or:    node grab.mjs --eval <file.cs>                         (runs any C# body file in the Editor)
import fs from 'node:fs';
import path from 'node:path';
import { spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const PROJECT = process.env.UPROJ || 'C:\\Users\\eduar\\OneDrive\\Documentos\\Github\\VR_Tutorial';
const a = process.argv.slice(2);

export function evalFile(file) {
  const r = spawnSync('unity', ['command', '--caller', 'plugin', '--skill', 'unity-cli', '--no-banner', '--format', 'json',
    '--project-path', PROJECT, 'eval_file', '--file', file, '--timeout', '120'], { encoding: 'utf8', shell: true });
  try {
    const j = JSON.parse(r.stdout);
    const res = j.data?.result;
    if (!j.success || !res?.success) return 'FAIL ' + JSON.stringify(j.errors?.length ? j.errors : res).slice(0, 1500);
    return res.result;
  } catch { return 'FAIL ' + (r.stdout + r.stderr).slice(0, 1500); }
}

if (!process.argv[1]?.endsWith('unity_eval.mjs')) {
  // imported as a module: only evalFile is used
} else if (a[0] === '--eval') {
  console.log(evalFile(path.resolve(a[1])));
} else {
  const [type, out, w = '0', h = '0', scroll = '-1', crop = '0'] = a;
  const code = fs.readFileSync(path.join(HERE, 'grab.cs.tmpl'), 'utf8')
    .replaceAll('__TYPE__', type).replaceAll('__OUT__', path.resolve(out).split(path.sep).join('/')).replaceAll('__W__', w).replaceAll('__H__', h).replaceAll('__SCROLL__', scroll).replaceAll('__CROP__', crop);
  const run = path.join(HERE, 'grab_run.cs');
  fs.writeFileSync(run, code);
  console.log(evalFile(run));
}
