import {spawn} from 'node:child_process';
const url='https://www.ib-cmty.com/timeline/m20230628161424524347851';
const child=spawn('C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe',['--headless=new','--disable-gpu','--no-first-run','--no-default-browser-check','--virtual-time-budget=8000','--dump-dom',url],{windowsHide:true});
let output=''; child.stdout.on('data',d=>output+=d); child.stderr.resume();
const timeout=setTimeout(()=>child.kill(),25000);
child.on('close',code=>{
 clearTimeout(timeout);
 const urls=[...output.matchAll(/<img[^>]+(?:src|data-src|data-original)=["']([^"']+)["']/g)].map(m=>m[1]).filter(x=>x.startsWith('http')&&!x.includes('/icon/')&&!x.includes('/emoji/'));
 console.log(JSON.stringify({code,length:output.length,images:[...new Set(urls)],title:output.match(/<title>(.*?)<\/title>/s)?.[1],body:output.replace(/<script[\s\S]*?<\/script>/gi,'').replace(/<style[\s\S]*?<\/style>/gi,'').replace(/<[^>]+>/g,' ').replace(/\s+/g,' ').slice(-1200)}));
});
