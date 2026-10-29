import assert from 'node:assert/strict';
import fs from 'node:fs/promises';
import {nextAction} from '../Automation/policy.mjs';
const runner=await fs.readFile(new URL('../Automation/runner.mjs',import.meta.url),'utf8');
const config={enabled:true};
const now=Date.parse('2026-10-26T08:00:00Z');
let checks=0;
for(const publisherStatus of ['validating','visual_review','building_android','waiting_for_desktop','waiting_for_player','failed','published']) {
  assert.equal(nextAction(config,{nextRunUtc:'2026-10-26T07:59:00Z',publisherStatus},{now}),'optimize');checks++;
  assert.equal(nextAction(config,{nextRunUtc:'2026-10-26T09:00:00Z',publisherStatus},{now}),'wait');checks++;
}
// The development entry must never invoke the publisher synchronously under its lease.
for(const forbidden of ['Publish-Iteration.ps1','Build-Android.ps1','deliverAndroid(','visual-review.json']) {
  assert.equal(runner.includes(forbidden),false,forbidden);checks++;
}
assert.equal(nextAction(config,{pendingSnapshot:{version:'done'},nextRunUtc:'2026-10-26T07:59:00Z'},{now}),'enqueue');checks++;
console.log(`DEVELOPMENT_ISOLATION_CHECKS_OK: ${checks} publication-independent scheduling and entrypoint checks.`);
