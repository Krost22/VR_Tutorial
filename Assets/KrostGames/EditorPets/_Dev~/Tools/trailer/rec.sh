#!/bin/bash
# rec.sh <clip> <window-expr> <frames> <actions C#>  -> records into scratchpad/frames/<clip>, waits until done
S="C:/Users/eduar/AppData/Local/Temp/claude/C--Users-eduar-OneDrive-Documentos-Github-VR-Tutorial/bc4afc4b-4d2c-4c0b-914a-cbb7bfad0675/scratchpad"
P='C:\Users\eduar\OneDrive\Documentos\Github\VR_Tutorial'
OUT="$S/frames/$1"; rm -rf "$OUT"; mkdir -p "$OUT"
node -e '
const fs=require("fs");const [t,out,win,total,acts,dst]=process.argv.slice(1);
let s=fs.readFileSync(t,"utf8").replace("OUTDIR",out).replace("WINDOW_EXPR",win).replace("TOTAL",total).replace("ACTIONS",acts);
fs.writeFileSync(dst,s)' "$S/rec_template.cs" "$OUT" "$2" "$3" "$4" "$S/rec_now.cs"
cd "C:/Users/eduar/OneDrive/Documentos/Github/VR_Tutorial"
unity command eval --project-path "$P" --no-banner --json --code "$(cat "$S/rec_now.cs")" 2>&1 | grep -o '"result": *"[^"]*"\|"message": *"[^"]*"'
for i in $(seq 1 120); do c=$(ls "$OUT" | wc -l); [ "$c" -ge "$3" ] && break; sleep 1; done
echo "$1: $(ls "$OUT" | wc -l) frames"
