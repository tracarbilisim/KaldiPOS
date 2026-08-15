const state={menu:null,config:null,cart:new Map(),query:"",activeCategory:null};
const el=id=>document.getElementById(id);
const money=v=>new Intl.NumberFormat("tr-TR",{minimumFractionDigits:0,maximumFractionDigits:2}).format(v)+" "+(state.config?.currency||"₺");
const sortedCategories=()=>state.menu.categories.slice().sort((a,b)=>a.sortOrder-b.sortOrder);
const sortedProducts=c=>c.products.slice().sort((a,b)=>a.sortOrder-b.sortOrder);
function tableFromUrl(){const path=decodeURIComponent(location.pathname);const m=path.match(/\/masa\/(\d+)/i);if(m)return m[1];const q=new URLSearchParams(location.search);return q.get("masa")||q.get("table")}
function normalize(s){return(s||"").toLocaleLowerCase("tr-TR").normalize("NFD").replace(/[\u0300-\u036f]/g,"")}
function categoryImage(c){return sortedProducts(c).find(p=>p.imagePath)?.imagePath||"/assets/kaldi-logo.png"}

async function boot(){
  try{
    const[m,c]=await Promise.all([fetch("/data/menu.json").then(r=>r.json()),fetch("/data/config.json").then(r=>r.json())]);
    state.menu=m;state.config=c;el("tagline").textContent=c.tagline||"";
    const table=tableFromUrl();if(table){el("tableBadge").hidden=false;el("tableBadge").textContent=`Masa ${table}`}
    el("cartButton").hidden=!c.orderingEnabled;el("noteWrap").hidden=!c.allowNotes;
    renderCategories();bind();
  }catch(e){console.error(e);el("categoryGrid").innerHTML='<div class="empty">Menü şu anda yüklenemedi. Lütfen tekrar deneyin.</div>'}
}

function renderCategories(){
  el("categoryGrid").innerHTML=sortedCategories().map(c=>`
    <button class="category-card" data-category="${c.sortOrder}" type="button">
      <img src="${categoryImage(c)}" alt="${esc(c.name)}" loading="lazy">
      <span class="category-shade"></span>
      <strong>${esc(c.name)}</strong>
    </button>`).join("");
  document.querySelectorAll("[data-category]").forEach(b=>b.onclick=()=>openCategory(Number(b.dataset.category)));
}

function renderCategoryNav(){
  el("categoryNav").innerHTML=sortedCategories().map(c=>`
    <button class="category-chip ${c.sortOrder===state.activeCategory?"active":""}" data-nav-category="${c.sortOrder}" type="button">
      <img src="${categoryImage(c)}" alt="">
      <span>${esc(c.name)}</span>
    </button>`).join("");
  document.querySelectorAll("[data-nav-category]").forEach(b=>b.onclick=()=>openCategory(Number(b.dataset.navCategory),false));
  requestAnimationFrame(()=>el("categoryNav").querySelector(".active")?.scrollIntoView({behavior:"smooth",inline:"center",block:"nearest"}));
}

function openCategory(sortOrder,scrollTop=true){
  const c=sortedCategories().find(x=>x.sortOrder===sortOrder);if(!c)return;
  state.activeCategory=sortOrder;state.query="";el("searchInput").value="";
  el("homeView").hidden=true;el("categoryView").hidden=false;el("backButton").hidden=false;el("topSpacer").hidden=true;
  renderCategoryNav();
  const image=categoryImage(c);
  el("categoryHero").innerHTML=`<img src="${image}" alt="${esc(c.name)}"><span></span><h1>${esc(c.name)}</h1>`;
  el("categoryTitle").textContent=c.name;el("categoryCount").textContent=`${c.products.length} ürün`;
  el("productList").innerHTML=sortedProducts(c).map(productRow).join("");
  bindAddButtons(el("productList"));
  if(scrollTop)window.scrollTo({top:0,behavior:"smooth"});
}

function goHome(){
  state.activeCategory=null;el("categoryView").hidden=true;el("homeView").hidden=false;el("backButton").hidden=true;el("topSpacer").hidden=false;
  window.scrollTo({top:0,behavior:"smooth"});
}

function productRow(p){
  const image=state.config.showImages&&p.imagePath
    ?`<div class="product-thumb"><img src="${p.imagePath}" alt="${esc(p.name)}" loading="lazy" onerror="this.parentElement.classList.add('image-error')"></div>`
    :`<div class="product-thumb logo-thumb"><img src="/assets/kaldi-logo.png" alt=""></div>`;
  const add=state.config.orderingEnabled?`<button class="add-button" data-add="${p.externalId}" aria-label="${esc(p.name)} sepete ekle">+</button>`:"";
  return `<article class="product-row">${image}<div class="product-info"><div class="product-top"><h3>${esc(p.name)}</h3><strong class="price">${money(p.price)}</strong></div>${p.description?`<p>${esc(p.description)}</p>`:""}${add}</div></article>`
}

function search(){
  state.query=el("searchInput").value;const q=normalize(state.query.trim());
  if(!q){el("searchResults").hidden=true;document.querySelector(".section-head").parentElement.hidden=false;return}
  const found=sortedCategories().flatMap(c=>sortedProducts(c).filter(p=>normalize(`${p.name} ${p.description||""} ${c.name}`).includes(q)));
  el("searchResults").hidden=false;el("searchCount").textContent=`${found.length} ürün`;
  el("searchProductList").innerHTML=found.length?found.map(productRow).join(""):'<div class="empty">Aramanıza uygun ürün bulunamadı.</div>';
  bindAddButtons(el("searchProductList"));
}

function allProducts(){return state.menu.categories.flatMap(c=>c.products)}
function bindAddButtons(root){root.querySelectorAll("[data-add]").forEach(b=>b.onclick=()=>add(b.dataset.add))}
function add(id){const p=allProducts().find(x=>String(x.externalId)===String(id));if(!p)return;const old=state.cart.get(id)||{product:p,qty:0};old.qty++;state.cart.set(id,old);updateCart();toast(`${p.name} sepete eklendi`)}
function change(id,delta){const row=state.cart.get(id);if(!row)return;row.qty+=delta;if(row.qty<=0)state.cart.delete(id);else state.cart.set(id,row);updateCart();renderCart()}
function updateCart(){let count=0;for(const x of state.cart.values())count+=x.qty;el("cartCount").textContent=count}
function renderCart(){const rows=[...state.cart.entries()];el("cartItems").innerHTML=rows.length?rows.map(([id,x])=>`<div class="cart-row"><div><h3>${esc(x.product.name)}</h3><small>${money(x.product.price)} × ${x.qty}</small></div><div class="qty"><button data-q="${id}" data-d="-1">−</button><strong>${x.qty}</strong><button data-q="${id}" data-d="1">+</button></div></div>`).join(""):'<div class="empty">Sepetiniz boş.</div>';el("cartTotal").textContent=money(rows.reduce((s,[,x])=>s+x.product.price*x.qty,0));el("cartItems").querySelectorAll("[data-q]").forEach(b=>b.onclick=()=>change(b.dataset.q,Number(b.dataset.d)))}
function openCart(){renderCart();el("cartSheet").hidden=false;document.body.style.overflow="hidden"}
function closeCart(){el("cartSheet").hidden=true;document.body.style.overflow=""}
async function submit(){if(!state.cart.size)return toast("Sepetiniz boş.");const table=tableFromUrl();if(!table)return toast("Sipariş için masa QR kodunu okutmalısınız.");const payload={table:Number(table),note:el("orderNote").value.trim(),items:[...state.cart.values()].map(x=>({externalId:x.product.externalId,quantity:x.qty,unitPrice:x.product.price}))};try{const r=await fetch("/api/orders",{method:"POST",headers:{"content-type":"application/json"},body:JSON.stringify(payload)});const d=await r.json();if(!r.ok)throw new Error(d.message||"Sipariş gönderilemedi");state.cart.clear();updateCart();closeCart();toast("Siparişiniz alındı.")}catch(e){toast(e.message||"Sipariş gönderilemedi.")}}
function bind(){el("searchInput").addEventListener("input",search);el("backButton").onclick=goHome;el("cartButton").onclick=openCart;document.querySelectorAll("[data-close-cart]").forEach(b=>b.onclick=closeCart);el("submitOrder").onclick=submit}
function toast(t){const x=el("toast");x.textContent=t;x.hidden=false;clearTimeout(window.__toast);window.__toast=setTimeout(()=>x.hidden=true,2600)}
function esc(s){return String(s??"").replace(/[&<>'"]/g,c=>({"&":"&amp;","<":"&lt;",">":"&gt;","'":"&#39;",'"':"&quot;"}[c]))}
document.addEventListener("DOMContentLoaded",boot);