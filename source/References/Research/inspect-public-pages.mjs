const pages = [
  'https://stzb.gamedreamer.com.tw/js/wjzl.js',
  'https://stzb.gamedreamer.com.tw/plate_page.html?num=0',
  'https://www.daisangoku.com/news/official/20221024/31170_1048138.html'
];
await Promise.all(pages.map(async url => {
  const response = await fetch(url, {signal: AbortSignal.timeout(15000)});
  const body = await response.text();
  console.log('\nURL ' + url + ' STATUS ' + response.status + ' LENGTH ' + body.length);
  if (url.includes('wjzl.js')) {
    const start = body.indexOf('[', body.indexOf('var wjzl'));
    const end = body.lastIndexOf(']');
    try {
      const rows = JSON.parse(body.slice(start, end + 1).split('\n').filter(line=>!line.trimStart().startsWith('//')).join('\n').replace(/,\s*([}\]])/g,'$1'));
      console.log(JSON.stringify(rows.map((r,index)=>({index,id:r.id,name:r.name})).filter(r=>/曹操|荀彧|皇甫|馬超|軻比|公孫|張遼/.test(r.name)).slice(0,24),null,2));
    } catch (error) { console.log('Metadata parse failed: ' + error.message); }
  }
  else console.log(body.split('\n').filter(line => /\.png|\.jpg|\.gif|\.webp|\.mp4|wjzl|quality|\.js/.test(line)).map(line=>line.slice(0,1400)).join('\n').slice(0,7000));
}));
