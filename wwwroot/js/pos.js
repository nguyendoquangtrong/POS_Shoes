// wwwroot/js/pos.js
class POSSystem {
    constructor() {
        this.cart = [];
        this.selectedProduct = null;
        this.selectedSize = null;
        this.init();
    }

    init() {
        this.bindEvents();
        this.loadProducts();
    }

    bindEvents() {
        // Search functionality
        $('#searchInput').on('keyup', debounce((e) => {
            this.searchProducts(e.target.value);
        }, 300));

        $('#searchBtn').on('click', () => {
            this.searchProducts($('#searchInput').val());
        });

        // Cart events
        $('#clearCartBtn').on('click', () => this.clearCart());
        $('#checkoutBtn').on('click', () => this.openCheckoutModal());

        // Modal events
        $('#addToCartBtn').on('click', () => this.addToCart());
        $('#confirmPaymentBtn').on('click', () => this.processPayment());

        // Quantity controls
        $('#decreaseQty').on('click', () => this.changeQuantity(-1));
        $('#increaseQty').on('click', () => this.changeQuantity(1));

        // Size selection
        $(document).on('click', '.size-option:not(.disabled)', (e) => {
            this.selectSize(e.currentTarget);
        });

        // Cart item controls
        $(document).on('click', '.remove-item', (e) => {
            const index = $(e.target).data('index');
            this.removeFromCart(index);
        });

        $(document).on('click', '.qty-decrease', (e) => {
            const index = $(e.target).data('index');
            this.updateCartItemQuantity(index, -1);
        });

        $(document).on('click', '.qty-increase', (e) => {
            const index = $(e.target).data('index');
            this.updateCartItemQuantity(index, 1);
        });
    }

    async loadProducts() {
        try {
            const response = await fetch('/Saler/Product/SearchProducts');
            const products = await response.json();
            this.renderProducts(products);
        } catch (error) {
            console.error('Error loading products:', error);
            this.showError('Lỗi khi tải danh sách sản phẩm');
        }
    }

    async searchProducts(searchTerm) {
        try {
            $('#productContainer').html('<div class="loading"><div class="spinner"></div></div>');

            const response = await fetch(`/Saler/Product/SearchProducts?search=${encodeURIComponent(searchTerm)}`);
            const products = await response.json();
            this.renderProducts(products);
        } catch (error) {
            console.error('Error searching products:', error);
            this.showError('Lỗi khi tìm kiếm sản phẩm');
        }
    }

    renderProducts(products) {
        const container = $('#productContainer');

        if (products.length === 0) {
            container.html(`
                <div class="col-12 text-center py-5">
                    <i class="fas fa-search fa-3x text-muted mb-3"></i>
                    <p class="text-muted">Không tìm thấy sản phẩm nào</p>
                </div>
            `);
            return;
        }

        const html = products.map(product => `
            <div class="col-lg-3 col-md-4 col-sm-6 mb-3 product-item" data-product-id="${product.productID}">
                <div class="product-card" onclick="pos.selectProduct('${product.productID}')">
                    <div class="product-image">
                        ${product.image ?
                `<img src="${product.image}" alt="${product.productName}" />` :
                `<div class="no-image"><i class="fas fa-image"></i></div>`
            }
                    </div>
                    <div class="product-info">
                        <h6 class="product-name">${product.productName}</h6>
                        <div class="product-price">${this.formatCurrency(product.price)}</div>
                        <div class="product-stock">
                            <small class="text-muted">Tồn: ${product.quantity}</small>
                        </div>
                        ${product.promotion ? '<div class="promotion-badge"><i class="fas fa-tag"></i> Khuyến mãi</div>' : ''}
                    </div>
                </div>
            </div>
        `).join('');

        container.html(html);
    }

    async selectProduct(productId) {
        try {
            const response = await fetch(`/Saler/Product/GetProductDetails?productId=${productId}`);
            const product = await response.json();

            this.selectedProduct = product;
            this.openSizeModal(product);
        } catch (error) {
            console.error('Error loading product details:', error);
            this.showError('Lỗi khi tải thông tin sản phẩm');
        }
    }

    openSizeModal(product) {
        // Populate modal with product info
        $('#modalProductImage').attr('src', product.image || '/images/no-image.png');
        $('#modalProductName').text(product.productName);
        $('#modalProductPrice').text(this.formatCurrency(product.price));
        $('#productQuantity').val(1);

        // Render size options
        const sizeHtml = product.sizes.map(size => `
            <div class="col-6 mb-2">
                <div class="size-option ${size.quantity === 0 ? 'disabled' : ''}" 
                     data-size-id="${size.sizeID}" 
                     data-size-name="${size.size}"
                     data-stock="${size.quantity}">
                    <div class="size-name">${size.size}</div>
                    <div class="size-stock">Còn: ${size.quantity}</div>
                </div>
            </div>
        `).join('');

        $('#sizeOptions').html(sizeHtml);

        // Reset selection
        this.selectedSize = null;
        $('.size-option').removeClass('selected');
        $('#addToCartBtn').prop('disabled', true);

        $('#sizeModal').modal('show');
    }

    selectSize(element) {
        $('.size-option').removeClass('selected');
        $(element).addClass('selected');

        this.selectedSize = {
            sizeId: $(element).data('size-id'),
            sizeName: $(element).data('size-name'),
            stock: $(element).data('stock')
        };

        $('#addToCartBtn').prop('disabled', false);
        this.updateMaxQuantity();
    }

    updateMaxQuantity() {
        if (this.selectedSize) {
            $('#productQuantity').attr('max', this.selectedSize.stock);

            // Adjust current quantity if it exceeds stock
            const currentQty = parseInt($('#productQuantity').val());
            if (currentQty > this.selectedSize.stock) {
                $('#productQuantity').val(this.selectedSize.stock);
            }
        }
    }

    changeQuantity(delta) {
        const input = $('#productQuantity');
        const current = parseInt(input.val());
        const min = parseInt(input.attr('min')) || 1;
        const max = parseInt(input.attr('max')) || 999;

        const newValue = Math.max(min, Math.min(max, current + delta));
        input.val(newValue);
    }

    addToCart() {
        if (!this.selectedProduct || !this.selectedSize) {
            this.showError('Vui lòng chọn sản phẩm và kích cỠ');
            return;
        }

        const quantity = parseInt($('#productQuantity').val());

        // Check if item already exists in cart
        const existingIndex = this.cart.findIndex(item =>
            item.productId === this.selectedProduct.productID &&
            item.sizeId === this.selectedSize.sizeId
        );

        if (existingIndex !== -1) {
            // Update existing item
            const newQuantity = this.cart[existingIndex].quantity + quantity;
            if (newQuantity <= this.selectedSize.stock) {
                this.cart[existingIndex].quantity = newQuantity;
                this.cart[existingIndex].total = newQuantity * this.selectedProduct.price;
            } else {
                this.showError('Số lượng vượt quá tồn kho');
                return;
            }
        } else {
            // Add new item
            const cartItem = {
                productId: this.selectedProduct.productID,
                productName: this.selectedProduct.productName,
                image: this.selectedProduct.image,
                price: this.selectedProduct.price,
                sizeId: this.selectedSize.sizeId,
                sizeName: this.selectedSize.sizeName,
                quantity: quantity,
                total: quantity * this.selectedProduct.price
            };
            this.cart.push(cartItem);
        }

        this.renderCart();
        $('#sizeModal').modal('hide');
        this.showSuccess('Đã thêm vào giỏ hàng');
    }

    renderCart() {
        const container = $('#cartItems');

        if (this.cart.length === 0) {
            container.html(`
                <div class="empty-cart text-center py-5">
                    <i class="fas fa-shopping-cart fa-3x text-muted mb-3"></i>
                    <p class="text-muted">Giỏ hàng trống</p>
                </div>
            `);
            $('#checkoutBtn').prop('disabled', true);
        } else {
            const html = this.cart.map((item, index) => `
                <div class="cart-item">
                    <div class="row align-items-center">
                        <div class="col-3">
                            <img src="${item.image || '/images/no-image.png'}" 
                                 alt="${item.productName}" class="cart-item-image">
                        </div>
                        <div class="col-6">
                            <div class="cart-item-info">
                                <h6>${item.productName}</h6>
                                <small class="text-muted">Size: ${item.sizeName}</small>
                                <div class="cart-item-price">${this.formatCurrency(item.price)}</div>
                            </div>
                        </div>
                        <div class="col-3">
                            <div class="quantity-controls">
                                <button class="qty-decrease" data-index="${index}">-</button>
                                <span>${item.quantity}</span>
                                <button class="qty-increase" data-index="${index}">+</button>
                            </div>
                            <button class="btn btn-sm btn-outline-danger mt-1 remove-item" data-index="${index}">
                                <i class="fas fa-trash"></i>
                            </button>
                        </div>
                    </div>
                </div>
            `).join('');

            container.html(html);
            $('#checkoutBtn').prop('disabled', false);
        }

        this.updateCartSummary();
    }

    updateCartSummary() {
        const totalQuantity = this.cart.reduce((sum, item) => sum + item.quantity, 0);
        const subtotal = this.cart.reduce((sum, item) => sum + item.total, 0);
        const discount = 0; // Implement discount logic if needed
        const grandTotal = subtotal - discount;

        $('#totalQuantity').text(totalQuantity);
        $('#subtotal').text(this.formatCurrency(subtotal));
        $('#discount').text(this.formatCurrency(discount));
        $('#grandTotal').text(this.formatCurrency(grandTotal));
    }

    removeFromCart(index) {
        this.cart.splice(index, 1);
        this.renderCart();
    }

    updateCartItemQuantity(index, delta) {
        const item = this.cart[index];
        const newQuantity = item.quantity + delta;

        if (newQuantity <= 0) {
            this.removeFromCart(index);
        } else {
            // Check stock limit (would need to store this info)
            item.quantity = newQuantity;
            item.total = newQuantity * item.price;
            this.renderCart();
        }
    }

    clearCart() {
        if (this.cart.length === 0) return;

        if (confirm('Bạn có chắc muốn xóa tất cả sản phẩm trong giỏ hàng?')) {
            this.cart = [];
            this.renderCart();
            this.showSuccess('Đã xóa tất cả sản phẩm');
        }
    }

    openCheckoutModal() {
        if (this.cart.length === 0) return;

        // Populate checkout items
        const itemsHtml = this.cart.map(item => `
            <div class="d-flex justify-content-between align-items-center py-2 border-bottom">
                <div>
                    <strong>${item.productName}</strong>
                    <small class="text-muted d-block">Size: ${item.sizeName} x ${item.quantity}</small>
                </div>
                <div class="text-end">
                    <div>${this.formatCurrency(item.total)}</div>
                </div>
            </div>
        `).join('');

        $('#checkoutItems').html(itemsHtml);

        const grandTotal = this.cart.reduce((sum, item) => sum + item.total, 0);
        $('#checkoutTotal').text(this.formatCurrency(grandTotal));

        $('#checkoutModal').modal('show');
    }

    async processPayment() {
        if (this.cart.length === 0) return;

        try {
            $('#confirmPaymentBtn').prop('disabled', true).html('<i class="fas fa-spinner fa-spin"></i> Đang xử lý...');

            const orderData = {
                totalPrice: this.cart.reduce((sum, item) => sum + item.total, 0),
                customerID: null, // Add customer selection if needed
                items: this.cart.map(item => ({
                    productID: item.productId,
                    sizeID: item.sizeId,
                    quantity: item.quantity,
                    price: item.price
                }))
            };

            const response = await fetch('/Saler/Sale/CreateOrder', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': $('[name=__RequestVerificationToken]').val()
                },
                body: JSON.stringify(orderData)
            });

            const result = await response.json();

            if (result.success) {
                this.showSuccess('Thanh toán thành công!');
                this.cart = [];
                this.renderCart();
                $('#checkoutModal').modal('hide');

                // Optionally print receipt or redirect
                this.printReceipt(result.orderId);
            } else {
                this.showError(result.message || 'Lỗi thanh toán');
            }
        } catch (error) {
            console.error('Payment error:', error);
            this.showError('Lỗi khi xử lý thanh toán');
        } finally {
            $('#confirmPaymentBtn').prop('disabled', false).html('<i class="fas fa-check"></i> Xác nhận thanh toán');
        }
    }

    printReceipt(orderId) {
        // Implement receipt printing logic
        console.log('Printing receipt for order:', orderId);
    }

    formatCurrency(amount) {
        return new Intl.NumberFormat('vi-VN', {
            style: 'currency',
            currency: 'VND'
        }).format(amount);
    }

    showSuccess(message) {
        // Show success toast/notification
        this.showToast(message, 'success');
    }

    showError(message) {
        // Show error toast/notification
        this.showToast(message, 'error');
    }

    showToast(message, type) {
        const toastHtml = `
            <div class="toast align-items-center text-white bg-${type === 'success' ? 'success' : 'danger'} border-0" role="alert">
                <div class="d-flex">
                    <div class="toast-body">${message}</div>
                    <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>
                </div>
            </div>
        `;

        const toastContainer = $('#toastContainer');
        if (toastContainer.length === 0) {
            $('body').append('<div id="toastContainer" class="toast-container position-fixed top-0 end-0 p-3"></div>');
        }

        const toast = $(toastHtml);
        $('#toastContainer').append(toast);

        const bsToast = new bootstrap.Toast(toast[0]);
        bsToast.show();

        toast.on('hidden.bs.toast', () => toast.remove());
    }
}

// Utility function
function debounce(func, wait) {
    let timeout;
    return function executedFunction(...args) {
        const later = () => {
            clearTimeout(timeout);
            func(...args);
        };
        clearTimeout(timeout);
        timeout = setTimeout(later, wait);
    };
}

// Initialize POS system when document is ready
$(document).ready(() => {
    window.pos = new POSSystem();
});
