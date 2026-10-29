export function due(state, now=Date.now()) {
  return !state.nextRunUtc || !Number.isFinite(Date.parse(state.nextRunUtc)) || Date.parse(state.nextRunUtc)<=now;
}
// Publication has its own queue and process; it cannot occupy the development lease.
export function nextAction(config,state,{held=false,now=Date.now(),probe=false}={}) {
  if(probe)return 'probe';
  if(!config.enabled)return 'disabled';
  if(held)return 'held';
  if(state.pendingSnapshot)return 'enqueue';
  if(due(state,now))return 'optimize';
  return 'wait';
}
export function validConfig(config) {
  return config && typeof config.enabled==='boolean' &&
    Number.isFinite(config.intervalHours)&&config.intervalHours>=1&&config.intervalHours<=24 &&
    Number.isInteger(config.workerTimeoutMinutes)&&config.workerTimeoutMinutes>=10&&config.workerTimeoutMinutes<=100 &&
    Number.isInteger(config.releaseTimeoutMinutes)&&config.releaseTimeoutMinutes>=5&&config.releaseTimeoutMinutes<=60 &&
    (!config.publishAndroid||(Number.isInteger(config.androidTimeoutMinutes)&&config.androidTimeoutMinutes>=10&&config.androidTimeoutMinutes<=60));
}
