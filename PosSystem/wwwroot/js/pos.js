const db = new Dexie('POSOfflineDB');
db.version(1).stores({
  products:    'id, sku, barcode, categoryId, branchId',
  categories:  'id, branchId',
  pendingOrders: '++localId, syncStatus, createdAt',
  stockSnapshot: 'productId, branchId',
  syncQueue:   '++id, entityType, syncStatus'
});

let _config = null;
let currentOrderType = 'DINE_IN';
let cart = [];
let currentOrderId = null;
let currentTable = null;
let currentPayMethod = 'CASH';
let currentProducts = [];
let currentProductsMap = new Map();

async function initPOS(config) {
  _config = config;
  console.log("POS Initialized", config);
  
  if (navigator.onLine) {
    // load from API
    await syncPull();
  }
  
  await loadProducts('all');
  
  // Setup SignalR
  setupSignalR();
}

async function syncPull() {
  try {
    const res = await fetch(`/api/sync/pull?branchId=${_config.branchId}`);
    if (res.ok) {
        const result = await res.json();
        // save to dexie
        if (result.data.products && result.data.products.length > 0) {
            await db.products.bulkPut(result.data.products);
        }
    }
  } catch (e) {
    console.warn("Pull failed", e);
  }
}

async function loadProducts(categoryId = 'all') {
  let products = [];
  if (navigator.onLine) {
    // try API
    try {
        const res = await fetch(`/api/orders/products?branchId=${_config.branchId}&categoryId=${categoryId === 'all' ? '' : categoryId}`);
        const result = await res.json();
        products = result.data || [];
    } catch {
        products = await getProductsFromDexie(categoryId);
    }
  } else {
    products = await getProductsFromDexie(categoryId);
  }
  currentProducts = products || [];
  currentProductsMap = new Map(currentProducts.map(p => [String(p.id).toLowerCase(), p]));
  renderProductGrid(products);
}

async function getProductsFromDexie(categoryId) {
  if (categoryId === 'all') return await db.products.toArray();
  return await db.products.where('categoryId').equals(categoryId).toArray();
}

function renderProductGrid(products) {
  const grid = document.getElementById('productGrid');
  if(!grid) return;
  
  if (!products || products.length === 0) {
      grid.innerHTML = '<div class="col-span-full text-center py-8 text-gray-400">ไม่มีสินค้า</div>';
      return;
  }
  
  grid.innerHTML = products.map(p => `
    <div class="bg-white rounded-xl shadow-sm border overflow-hidden cursor-pointer hover:border-blue-500 transition-all"
         onclick="addToCart('${p.id}')" data-product="${p.id}">
      <div class="h-24 bg-gray-100 flex items-center justify-center">
        ${p.imageUrl ? `<img src="${p.imageUrl}" class="w-full h-full object-cover"/>` : '<i data-lucide="image" class="w-8 h-8 text-gray-300"></i>'}
      </div>
      <div class="p-3">
        <h4 class="text-sm font-semibold text-gray-800 line-clamp-1">${p.name}</h4>
        <div class="flex justify-between items-center mt-2">
          <span class="text-blue-600 font-bold text-sm">฿${p.price.toFixed(2)}</span>
          <span class="stock-badge text-xs px-2 py-1 bg-gray-100 rounded-full text-gray-600">${p.availableQty || 0}</span>
        </div>
      </div>
    </div>
  `).join('');
  lucide.createIcons();
}

async function addToCart(productId, variantId = null, note = '') {
  let product = currentProductsMap.get(String(productId).toLowerCase());
  if (!product) {
    product = await db.products.get(productId);
  }
  if (!product) {
    alert('ไม่พบข้อมูลสินค้า');
    return;
  }

  const existing = cart.find(x => x.productId === productId);
  if (existing) {
    existing.qty += 1;
    renderCart();
    return;
  }

  cart.push({
    id: Date.now().toString(),
    productId,
    name: product.name,
    price: Number(product.price || 0),
    qty: 1,
    note: note || ''
  });
  renderCart();
}

function renderCart() {
  const cartItems = document.getElementById('cartItems');
  const emptyCart = document.getElementById('emptyCart');
  
  if (cart.length === 0) {
    cartItems.innerHTML = emptyCart.outerHTML;
    document.getElementById('emptyCart').style.display = 'block';
  } else {
    document.getElementById('emptyCart').style.display = 'none';
    cartItems.innerHTML = cart.map(item => `
      <div class="flex justify-between items-center bg-gray-50 p-2 rounded-lg border">
        <div>
            <div class="text-sm font-medium text-gray-800">${item.name}</div>
            <div class="text-xs text-blue-600">฿${item.price.toFixed(2)} x ${item.qty}</div>
        </div>
        <button onclick="removeFromCart('${item.id}')" class="text-red-500 hover:text-red-700">
            <i data-lucide="trash-2" class="w-4 h-4"></i>
        </button>
      </div>
    `).join('');
  }
  calcTotals();
  lucide.createIcons();
}

function removeFromCart(id) {
    cart = cart.filter(c => c.id !== id);
    renderCart();
}

function calcTotals() {
  const subtotal = cart.reduce((sum, item) => sum + (item.price * item.qty), 0);
  const tax = subtotal * (_config.taxRate / 100);
  const total = subtotal + tax; // ignoring discount for now
  
  document.getElementById('summarySubtotal').innerText = `฿${subtotal.toFixed(2)}`;
  document.getElementById('summaryTax').innerText = `฿${tax.toFixed(2)}`;
  document.getElementById('summaryTotal').innerText = `฿${total.toFixed(2)}`;
  
  const payTotal = document.getElementById('payTotal');
  if(payTotal) payTotal.innerText = `฿${total.toFixed(2)}`;
}

function setOrderType(type) {
  currentOrderType = type;
  document.querySelectorAll('.order-type-btn').forEach(b => {
    b.classList.remove('active', 'bg-blue-50', 'text-blue-700');
  });
  const btn = document.getElementById(`type-${type}`);
  if (btn) btn.classList.add('active', 'bg-blue-50', 'text-blue-700');
  
  const tableSelector = document.getElementById('tableSelectorRow');
  if (tableSelector) {
      tableSelector.style.display = type === 'DINE_IN' ? 'block' : 'none';
  }
}

function openTableModal() {
  document.getElementById('tableModal').classList.remove('hidden');
}
function closeTableModal() {
  document.getElementById('tableModal').classList.add('hidden');
}
function selectTable(id, number, status) {
  if (status !== 'AVAILABLE') return;
  currentTable = id;
  document.getElementById('tableLabel').innerText = `โต๊ะ ${number}`;
  closeTableModal();
}

function filterCategory(catId) {
    loadProducts(catId);
}
function filterProducts(search) {
    // simple client side filter
    const cards = document.querySelectorAll('#productGrid > div');
    cards.forEach(card => {
        const name = card.querySelector('h4').innerText.toLowerCase();
        if (name.includes(search.toLowerCase())) card.style.display = 'block';
        else card.style.display = 'none';
    });
}

async function confirmOrder() {
    if (cart.length === 0) return alert('กรุณาเลือกสินค้า');
    if (currentOrderType === 'DINE_IN' && !currentTable) return alert('กรุณาเลือกโต๊ะ');

    try {
        const payload = {
            orderType: currentOrderType,
            tableId: currentTable,
            taxRate: Number(_config.taxRate || 7),
            cart: cart.map(x => ({ productId: x.productId, qty: x.qty, price: x.price, note: x.note }))
        };
        const res = await fetch('/api/orders/kitchen', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });
        const result = await res.json();
        if (res.ok) {
            alert(result.message);
            cart = [];
            renderCart();
            currentTable = null;
            if (document.getElementById('tableLabel')) document.getElementById('tableLabel').innerText = 'เลือกโต๊ะ';
        } else {
            alert(result.message || 'เกิดข้อผิดพลาด');
        }
    } catch (e) {
        alert('เกิดข้อผิดพลาดในการส่งข้อมูล');
    }
}

function cancelOrder() {
    if (cart.length === 0) return;
    if (confirm('คุณต้องการยกเลิกรายการในตะกร้าทั้งหมดหรือไม่?')) {
        cart = [];
        renderCart();
        currentTable = null;
        if (document.getElementById('tableLabel')) document.getElementById('tableLabel').innerText = 'เลือกโต๊ะ';
    }
}

function openPayment() {
    if(cart.length === 0) return alert('กรุณาเลือกสินค้า');
    document.getElementById('paymentModal').classList.remove('hidden');
}
function closePayment() {
    document.getElementById('paymentModal').classList.add('hidden');
}

function setPayMethod(method, el = null) {
    currentPayMethod = method;
    document.querySelectorAll('.pay-method-btn').forEach(b => b.classList.remove('bg-blue-50', 'border-blue-500'));
    if (el) el.classList.add('bg-blue-50', 'border-blue-500');
    document.getElementById('cashFields').classList.toggle('hidden', method !== 'CASH');
    document.getElementById('refFields').classList.toggle('hidden', method === 'CASH');
}

function calcChange() {
    const received = parseFloat(document.getElementById('cashReceived').value) || 0;
    const total = cart.reduce((sum, item) => sum + (item.price * item.qty), 0) * 1.07;
    const change = received - total;
    document.getElementById('changeAmount').innerText = `฿${Math.max(0, change).toFixed(2)}`;
}

async function processPayment() {
    if (!navigator.onLine) {
        // Offline payment save
        await db.pendingOrders.add({
            localId: Date.now().toString(),
            cart: cart,
            total: cart.reduce((sum, item) => sum + (item.price * item.qty), 0) * 1.07,
            syncStatus: 'PENDING',
            createdAt: new Date().toISOString()
        });
        alert('ชำระเงินเรียบร้อย (บันทึกออฟไลน์)');
        closePayment();
        cart = [];
        renderCart();
        return;
    }
    
    // Online
    try {
        const taxRate = Number(_config.taxRate || 7);
        const subtotal = cart.reduce((sum, item) => sum + (item.price * item.qty), 0);
        const total = subtotal + (subtotal * taxRate / 100);
        const payload = {
          orderType: currentOrderType,
          tableId: currentOrderType === 'DINE_IN' ? currentTable : null,
          paymentMethod: currentPayMethod,
          cashReceived: currentPayMethod === 'CASH' ? Number(document.getElementById('cashReceived').value || 0) : null,
          referenceNo: currentPayMethod === 'CASH' ? null : document.getElementById('refNumber').value,
          taxRate,
          cart: cart.map(x => ({
            productId: x.productId,
            qty: x.qty,
            price: x.price,
            note: x.note || null
          }))
        };
        const res = await fetch(`/api/orders/create`, {
          method: 'POST',
          headers: {'Content-Type':'application/json'},
          body: JSON.stringify(payload)
        });
        if(res.ok) {
            alert('ชำระเงินเรียบร้อย');
            closePayment();
            cart = [];
            renderCart();
            currentTable = null;
            const tableLabel = document.getElementById('tableLabel');
            if (tableLabel) tableLabel.innerText = 'เลือกโต๊ะ';
            await loadProducts('all');
        } else {
            const err = await res.json();
            alert(err.message || 'เกิดข้อผิดพลาดในการชำระเงิน');
        }
    } catch(e) {
        alert('เกิดข้อผิดพลาด');
    }
}

let barcodeBuffer = '';
let barcodeTimer = null;
document.addEventListener('keydown', e => {
  if (e.target.tagName === 'INPUT') return;
  if (e.key === 'Enter' && barcodeBuffer.length > 3) {
    // searchByBarcode(barcodeBuffer);
    barcodeBuffer = '';
    return;
  }
  if (e.key.length === 1) barcodeBuffer += e.key;
  clearTimeout(barcodeTimer);
  barcodeTimer = setTimeout(() => barcodeBuffer = '', 100);
});

// SignalR connection
function setupSignalR() {
    if(typeof signalR === 'undefined') return;
    const connection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/pos')
      .withAutomaticReconnect()
      .build();

    connection.on('StockUpdated', (productId, newQty) => {
      const card = document.querySelector(`[data-product="${productId}"]`);
      if (card) {
          const badge = card.querySelector('.stock-badge');
          if(badge) badge.textContent = newQty;
      }
    });

    connection.start().then(() => {
        console.log("SignalR Connected");
    }).catch(err => console.error(err));
}

// Auto sync when back online
window.addEventListener('online', async () => {
  const pending = await db.pendingOrders.where('syncStatus').equals('PENDING').toArray();
  for (const order of pending) {
    try {
      const res = await fetch('/api/sync/push', {
        method: 'POST',
        headers: {'Content-Type':'application/json'},
        body: JSON.stringify({ orders: [order] })
      });
      if (res.ok) {
          await db.pendingOrders.update(order.localId, { syncStatus: 'SYNCED' });
      }
    } catch {}
  }
});
