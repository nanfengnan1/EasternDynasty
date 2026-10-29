const pages = [
'https://stzb.gamedreamer.com.tw/NewsContent/zh_TW/stzb2_event_3156.html',
'https://www.daisangoku.com/news/update/20230721/31220_1100067.html',
'https://www.daisangoku.com/news/update/20250220/31220_1213285.html',
'https://stzb.gamedreamer.com.tw/NewsContent/zh_TW/stzb2_notice_1976.html',
'https://stzb.gamedreamer.com.tw/NewsContent/zh_TW/stzb2_news_6326.html',
'https://www.ib-cmty.com/timeline/m20230628161424524347851'
];
await Promise.all(pages.map(async url=>{
 const response=await fetch(url,{signal:AbortSignal.timeout(15000)});
 const body=await response.text();
 const urls=[...body.matchAll(/(?:src|data-src|data-original)=["']([^"']+)["']/g)].map(x=>new URL(x[1],url).href).filter(x=>/\.(?:jpe?g|png|webp)/i.test(x));
 console.log(JSON.stringify({page:url,status:response.status,images:[...new Set(urls)]}));
}));
