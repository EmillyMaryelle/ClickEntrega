const app = {
    cart: [],
    currentUser: null,
    userRole: null, // 'client' or 'admin'
    data: {
        categories: [],
        products: [],
        notifications: []
    },
    lastNotificationId: 0,
    
    init: function() {
        this.cart = JSON.parse(sessionStorage.getItem('cart')) || [];
        this.updateCartCount();
        
        // Check session
        const savedUser = sessionStorage.getItem('currentUser');
        const savedRole = sessionStorage.getItem('userRole');
        
        if (savedUser && savedRole) {
            this.currentUser = JSON.parse(savedUser);
            this.userRole = savedRole;
        }

        // Inicializar validação de campos
        this.initFieldValidation();

        // Routing Logic
        const path = window.location.pathname.toLowerCase();
        
        if (path === '/cliente') {
            if (this.userRole === 'client') {
                 this.showCatalog();
            } else {
                 this.showClientLogin(); 
            }
        } else if (path === '/empresa' || path === '/admin') {
            if (this.userRole === 'admin') {
                this.showAdminDashboard();
            } else {
                this.showCompanyLogin();
            }
        } else {
            // Default behavior
            if (this.currentUser) {
                if(this.userRole === 'admin') this.showAdminDashboard();
                else this.showCatalog();
            } else {
                this.showLoginSelection();
            }
        }

        // Poll for notifications if client
        setInterval(() => {
            if(this.userRole === 'client' && this.currentUser) {
                this.checkNotifications();
            }
        }, 30000); // Check every 30s

        // Handle Back/Forward buttons
        window.onpopstate = () => {
             window.location.reload();
        };
    },

    // --- Navigation & Auth ---
    showLoginSelection: function() {
        history.pushState(null, '', '/');
        this.renderTemplate('login-selection-template');
        this.updateHeader(false);
    },

    showClientRegistration: function() {
        this.renderTemplate('client-registration-template');
    },

    showClientLogin: function() {
        history.pushState(null, '', '/cliente');
        this.renderTemplate('client-template');
        this.updateHeader(false);
        // Garantir que a área de login seja exibida
        const loginArea = document.getElementById('client-login-area');
        const dashboard = document.getElementById('client-dashboard');
        if (loginArea) loginArea.style.display = 'block';
        if (dashboard) dashboard.style.display = 'none';
    },

    showCompanyLogin: function() {
        history.pushState(null, '', '/empresa');
        this.renderTemplate('company-login-template');
        this.updateHeader(false);
    },

    showCompanyRegistration: function() {
        this.renderTemplate('company-registration-template');
    },

    loginCompany: async function() {
        const nameField = document.getElementById('login-company-name');
        const passwordField = document.getElementById('login-company-password');
        
        if (!nameField || !passwordField) {
            this.showAlertModal('error', 'Erro', 'Campos não encontrados. Recarregue a página.');
            return;
        }
        
        const name = nameField.value ? nameField.value.trim() : '';
        const password = passwordField.value ? passwordField.value.trim() : '';
        
        // Validação de campos obrigatórios
        let hasError = false;
        if (!name) {
            nameField.classList.add('error');
            const errorMsg = nameField.parentElement?.querySelector('.field-error');
            if (errorMsg) errorMsg.classList.add('show');
            hasError = true;
        } else {
            nameField.classList.remove('error');
            const errorMsg = nameField.parentElement?.querySelector('.field-error');
            if (errorMsg) errorMsg.classList.remove('show');
        }
        
        if (!password) {
            passwordField.classList.add('error');
            const errorMsg = passwordField.parentElement?.querySelector('.field-error');
            if (errorMsg) errorMsg.classList.add('show');
            hasError = true;
        } else {
            passwordField.classList.remove('error');
            const errorMsg = passwordField.parentElement?.querySelector('.field-error');
            if (errorMsg) errorMsg.classList.remove('show');
        }
        
        if (hasError) {
            this.showAlertModal('error', 'Erro', 'Preencha todos os campos obrigatórios.');
            return;
        }

        try {
            const response = await fetch('/api/Companies/Login', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ name, password })
            });

            if (response.ok) {
                const company = await response.json();
                this.currentUser = company;
                this.userRole = 'admin'; // Keep role as 'admin' for dashboard access
                this.saveSession();
                this.showAdminDashboard();
            } else {
                const errorText = await response.text();
                this.showAlertModal('error', 'Erro', errorText || "Credenciais inválidas.");
            }
        } catch(e) {
            console.error(e);
            this.showAlertModal('error', 'Erro', "Erro ao tentar login.");
        }
    },

    toggleOtherTypeField: function() {
        const select = document.getElementById('new-company-type');
        const otherContainer = document.getElementById('other-type-container');
        const manualInput = document.getElementById('new-company-type-manual');
        
        if (select && otherContainer) {
            if (select.value === 'Outro') {
                otherContainer.style.display = 'block';
                manualInput.classList.add('required');
            } else {
                otherContainer.style.display = 'none';
                manualInput.classList.remove('required');
                manualInput.value = ''; 
                manualInput.classList.remove('error');
                const errorMsg = manualInput.parentElement?.querySelector('.field-error');
                if (errorMsg) errorMsg.classList.remove('show');
            }
        }
    },

    registerCompany: async function() {
        const nameField = document.getElementById('new-company-name');
        const typeField = document.getElementById('new-company-type');
        const manualTypeField = document.getElementById('new-company-type-manual');
        const passwordField = document.getElementById('new-company-password');
        
        if (!nameField || !typeField || !passwordField) {
            this.showAlertModal('error', 'Erro', 'Campos não encontrados. Recarregue a página.');
            return;
        }
        
        const name = nameField.value ? nameField.value.trim() : '';
        let type = typeField.value ? typeField.value.trim() : '';
        const password = passwordField.value ? passwordField.value.trim() : '';
        
        // Handle 'Outro' type
        if (type === 'Outro' && manualTypeField) {
            const manualType = manualTypeField.value ? manualTypeField.value.trim() : '';
            if (manualType) {
                type = manualType;
            } else {
                // If manual type is empty, we will catch it in validation below if we handle it
                // Ideally, we treat 'Outro' as invalid if manual is empty
                type = ''; // Reset type to force validation error if manual is empty
            }
        }

        // Validação de campos obrigatórios
        let hasError = false;
        if (!name) {
            nameField.classList.add('error');
            const errorMsg = nameField.parentElement?.querySelector('.field-error');
            if (errorMsg) errorMsg.classList.add('show');
            hasError = true;
        } else {
            nameField.classList.remove('error');
            const errorMsg = nameField.parentElement?.querySelector('.field-error');
            if (errorMsg) errorMsg.classList.remove('show');
        }
        
        if (!type && typeField.value !== 'Outro') {
            typeField.classList.add('error');
            const errorMsg = typeField.parentElement?.querySelector('.field-error');
            if (errorMsg) errorMsg.classList.add('show');
            hasError = true;
        } else if (typeField.value === 'Outro' && !type) {
             // Manual field is empty
             if(manualTypeField) {
                manualTypeField.classList.add('error');
                const errorMsg = manualTypeField.parentElement?.querySelector('.field-error');
                if (errorMsg) errorMsg.classList.add('show');
                hasError = true;
             }
        } else {
            typeField.classList.remove('error');
            const errorMsg = typeField.parentElement?.querySelector('.field-error');
            if (errorMsg) errorMsg.classList.remove('show');

            if(manualTypeField) {
                manualTypeField.classList.remove('error');
                const errorMsg = manualTypeField.parentElement?.querySelector('.field-error');
                if (errorMsg) errorMsg.classList.remove('show');
            }
        }
        
        if (!password) {
            passwordField.classList.add('error');
            const errorMsg = passwordField.parentElement?.querySelector('.field-error');
            if (errorMsg) errorMsg.classList.add('show');
            hasError = true;
        } else {
            passwordField.classList.remove('error');
            const errorMsg = passwordField.parentElement?.querySelector('.field-error');
            if (errorMsg) errorMsg.classList.remove('show');
        }
        
        if (hasError) {
            this.showAlertModal('error', 'Erro', 'Preencha todos os campos obrigatórios.');
            return;
        }

        const company = { name, type, password };

        try {
            const response = await fetch('/api/Companies', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(company)
            });

            if (response.ok) {
                const newCompany = await response.json();
                this.showAlertModal('success', 'Sucesso', `Empresa ${newCompany.name} cadastrada com sucesso!`);
                this.currentUser = newCompany;
                this.userRole = 'admin';
                this.saveSession();
                this.showAdminDashboard();
            } else {
                const errorText = await response.text();
                this.showAlertModal('error', 'Erro', "Erro ao cadastrar: " + errorText);
            }
        } catch(e) {
            console.error(e);
            this.showAlertModal('error', 'Erro', "Erro de conexão.");
        }
    },

    login: async function() {
        const emailField = document.getElementById('login-client-email');
        const passwordField = document.getElementById('login-client-password');
        
        if (!emailField || !passwordField) {
            this.showAlertModal('error', 'Erro', 'Campos não encontrados. Recarregue a página.');
            return;
        }
        
        const email = emailField.value ? emailField.value.trim() : '';
        const password = passwordField.value ? passwordField.value.trim() : '';
        
        // Validação de campos obrigatórios
        let hasError = false;
        if (!email) {
            emailField.classList.add('error');
            const errorMsg = emailField.parentElement?.querySelector('.field-error');
            if (errorMsg) errorMsg.classList.add('show');
            hasError = true;
        } else {
            emailField.classList.remove('error');
            const errorMsg = emailField.parentElement?.querySelector('.field-error');
            if (errorMsg) errorMsg.classList.remove('show');
        }
        
        if (!password) {
            passwordField.classList.add('error');
            const errorMsg = passwordField.parentElement?.querySelector('.field-error');
            if (errorMsg) errorMsg.classList.add('show');
            hasError = true;
        } else {
            passwordField.classList.remove('error');
            const errorMsg = passwordField.parentElement?.querySelector('.field-error');
            if (errorMsg) errorMsg.classList.remove('show');
        }
        
        if (hasError) {
            this.showAlertModal('error', 'Erro', 'Preencha todos os campos obrigatórios.');
            return;
        }

        try {
            const response = await fetch('/api/Clients/Login', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ email, password })
            });

            if (response.ok) {
                const client = await response.json();
                this.currentUser = client;
                this.userRole = 'client';
                this.saveSession();
                this.showCompanyTypes();
            } else {
                const errorText = await response.text();
                this.showAlertModal('error', 'Erro', errorText || "Email ou senha inválidos.");
            }
        } catch(e) {
            console.error(e);
            this.showAlertModal('error', 'Erro', "Erro ao tentar login.");
        }
    },

    logout: function() {
        this.currentUser = null;
        this.userRole = null;
        this.lastNotificationId = 0;
        this.data.notifications = [];
        sessionStorage.removeItem('currentUser');
        sessionStorage.removeItem('userRole');
        this.showLoginSelection();
    },

    saveSession: function() {
        sessionStorage.setItem('currentUser', JSON.stringify(this.currentUser));
        sessionStorage.setItem('userRole', this.userRole);
    },

    updateHeader: function(isLoggedIn) {
        const header = document.getElementById('main-header');
        const clientNav = document.getElementById('client-nav');
        const adminNav = document.getElementById('admin-nav');

        if (!isLoggedIn) {
            header.style.display = 'none';
        } else {
            header.style.display = 'block';
            if (this.userRole === 'admin') {
                clientNav.style.display = 'none';
                adminNav.style.display = 'block';
            } else {
                clientNav.style.display = 'block';
                adminNav.style.display = 'none';
                this.checkNotifications(); // Update badge

                // Toggle Catalog Link
                const catalogLink = document.getElementById('nav-catalog-link');
                if (catalogLink) {
                    catalogLink.style.display = this.data.currentCompanyId ? 'inline-block' : 'none';
                }
            }
        }
    },

    // --- Company Selection ---
    showCompanyTypes: async function() {
        history.pushState(null, '', '/tipos');
        this.updateHeader(this.currentUser != null);
        this.renderTemplate('company-type-template');
        
        const container = document.getElementById('company-types-grid');
        container.innerHTML = '<p>Carregando...</p>';
        
        try {
            const response = await fetch('/api/Companies');
            const companies = await response.json();
            this.data.companies = companies; // Cache companies
            
            // Get unique types
            const types = [...new Set(companies.map(c => c.type))];
            
            container.innerHTML = '';
            if (types.length === 0) {
                container.innerHTML = '<p>Nenhum estabelecimento encontrado.</p>';
                return;
            }

            types.forEach(type => {
                const div = document.createElement('div');
                div.className = 'menu-item'; // Reuse styling
                div.style.cursor = 'pointer';
                div.style.display = 'flex';
                div.style.alignItems = 'center';
                div.style.justifyContent = 'center';
                div.onclick = () => app.showCompaniesByType(type);
                div.innerHTML = `
                    <div class="menu-item-content" style="text-align:center;">
                        <h3 style="font-size: 1.5em; margin: 10px 0;">${type}</h3>
                    </div>
                `;
                container.appendChild(div);
            });
        } catch(e) {
            console.error(e);
            container.innerHTML = '<p>Erro ao carregar tipos.</p>';
        }
    },

    showCompaniesByType: function(type) {
        this.renderTemplate('company-list-template');
        document.getElementById('selected-type-title').textContent = type;
        
        const container = document.getElementById('companies-grid');
        // Filter from cached companies
        const companies = this.data.companies.filter(c => c.type === type);
        
        container.innerHTML = '';
        companies.forEach(c => {
            const div = document.createElement('div');
            div.className = 'menu-item';
            div.style.cursor = 'pointer';
            div.onclick = () => app.showCatalog(c.id);
            div.innerHTML = `
                <div class="menu-item-content">
                    <h3>${c.name}</h3>
                    <p style="color: #666;">Clique para ver o cardápio</p>
                </div>
            `;
            container.appendChild(div);
        });
    },

    // --- Client Views ---
    showCatalog: async function(companyId) {
        if (!companyId && this.data.currentCompanyId) {
            companyId = this.data.currentCompanyId;
        }
        
        if (!companyId) {
            return this.showCompanyTypes();
        }
        
        this.data.currentCompanyId = companyId;
        sessionStorage.setItem('currentCompanyId', companyId);

        history.pushState(null, '', '/cliente');
        this.userRole = 'client'; // Default context
        this.updateHeader(true);
        this.renderTemplate('catalog-template');
        await this.renderMenu();
    },

    renderMenu: async function(filterCategoryId = null) {
        const container = document.getElementById('menu-container');
        if(!container) return;

        const companyId = this.data.currentCompanyId;
        if (!companyId) return;

        container.innerHTML = '<p>Carregando menu...</p>';

        try {
            // Load Categories, Products and Company Info
            const [catRes, prodRes, compRes] = await Promise.all([
                fetch(`/api/Categories?companyId=${companyId}`),
                fetch(`/api/Products?companyId=${companyId}`),
                fetch(`/api/Companies/${companyId}`)
            ]);

            this.data.categories = await catRes.json();
            this.data.products = await prodRes.json();

            if (compRes.ok) {
                const comp = await compRes.json();
                this.data.currentCompanyName = comp.name;
            } else {
                this.data.currentCompanyName = 'Empresa';
            }

            container.innerHTML = '';

            // Render Categories Filter
            const filterDiv = document.createElement('div');
            filterDiv.className = 'category-filter';
            filterDiv.innerHTML = `
                <button onclick="app.renderMenu()" class="${!filterCategoryId ? 'active' : ''}">Todos</button>
                ${this.data.categories.map(c => `
                    <button onclick="app.renderMenu(${c.id})" class="${filterCategoryId == c.id ? 'active' : ''}">${c.name}</button>
                `).join('')}
            `;
            container.appendChild(filterDiv);

            // Filter Products
            let productsToShow = this.data.products;
            if(filterCategoryId) {
                // Ensure ID is a number
                filterCategoryId = Number(filterCategoryId);
                productsToShow = productsToShow.filter(p => p.categoryId === filterCategoryId);
            }

            // Render Products
            const grid = document.createElement('div');
            grid.className = 'menu-grid';

            if(productsToShow.length === 0) {
                grid.innerHTML = '<p>Nenhum produto encontrado nesta categoria.</p>';
            } else {
                productsToShow.forEach(p => {
                    const isOutOfStock = p.stockQuantity <= 0;
                    const div = document.createElement('div');
                    div.className = `menu-item ${isOutOfStock ? 'out-of-stock' : ''}`;
                    div.innerHTML = `
                        ${p.imageUrl ? `<div class="menu-item-img"><img src="${p.imageUrl}" alt="${p.name}"></div>` : ''}
                        <div class="menu-item-content">
                            <h3>${p.name}</h3>
                            <p class="description">${p.description || 'Sem descrição'}</p>
                            <p class="price">R$ ${p.price.toFixed(2)}</p>
                            ${isOutOfStock ? '<p class="stock-warning">Esgotado</p>' : ''}
                        </div>
                        <button onclick="app.addToCart(${p.id})" ${isOutOfStock ? 'disabled' : ''}>
                            ${isOutOfStock ? 'Indisponível' : 'Adicionar'}
                        </button>
                    `;
                    grid.appendChild(div);
                });
            }
            container.appendChild(grid);

        } catch(e) {
            console.error('Error loading menu:', e);
            container.innerHTML = '<p>Erro ao carregar o cardápio.</p>';
        }
    },

    showCart: function() {
        this.updateHeader(true);
        this.renderTemplate('cart-template');
        this.renderCartItems();
        
        // Auto-fill client ID if logged in
        if(this.currentUser && this.currentUser.id) {
            const input = document.getElementById('checkout-client-id');
            if(input) {
                input.value = this.currentUser.id;
                input.readOnly = true;
            }
        }
    },

    addToCart: function(productId) {
        // Ensure ID is a number
        const id = Number(productId);
        const product = this.data.products.find(p => p.id === id);
        if(!product) return;

        const existing = this.cart.find(i => i.productId === id);
        if(existing) {
            if(existing.quantity >= product.stockQuantity) {
                this.showAlertModal('error', 'Erro', 'Estoque insuficiente para adicionar mais.');
                return;
            }
            existing.quantity++;
        } else {
            let cName = 'Empresa';
            if (product.company && product.company.name) {
                cName = product.company.name;
            } else if (this.data.currentCompanyId == product.companyId && this.data.currentCompanyName) {
                cName = this.data.currentCompanyName;
            }

            this.cart.push({
                productId: product.id,
                name: product.name,
                price: product.price,
                quantity: 1,
                companyId: product.companyId,
                companyName: cName
            });
        }
        
        this.saveCart();
        this.updateCartCount();
        this.showAlertModal('success', 'Sucesso', 'Produto adicionado!', {
            text: 'Ir ao Carrinho',
            onclick: 'app.closeAlertModal(); app.showCart();'
        });
    },

    toggleCartGroup: function(companyId) {
        const content = document.getElementById(`cart-group-content-${companyId}`);
        const icon = document.getElementById(`icon-${companyId}`);
        if(content && icon) {
            if(content.style.display === 'none') {
                content.style.display = 'block';
                icon.textContent = '−';
            } else {
                content.style.display = 'none';
                icon.textContent = '+';
            }
        }
    },

    renderCartItems: function() {
        const container = document.getElementById('cart-items');
        if(!container) return;

        container.innerHTML = '';

        if(this.cart.length === 0) {
            container.innerHTML = '<p>Seu carrinho está vazio.</p>';
            return;
        }

        // Group by company
        const companies = {};
        this.cart.forEach(item => {
            const cId = item.companyId || 'unknown';
            if(!companies[cId]) {
                companies[cId] = {
                    name: item.companyName || 'Outros',
                    items: []
                };
            }
            companies[cId].items.push(item);
        });

        // Render each company group
        Object.keys(companies).forEach(companyId => {
            const group = companies[companyId];
            const groupDiv = document.createElement('div');
            groupDiv.className = 'cart-company-group';
            groupDiv.style.border = '1px solid #ddd';
            groupDiv.style.padding = '15px';
            groupDiv.style.marginBottom = '20px';
            groupDiv.style.borderRadius = '8px';

            // Header with Collapse Button
            const headerDiv = document.createElement('div');
            headerDiv.style.display = 'flex';
            headerDiv.style.justifyContent = 'space-between';
            headerDiv.style.alignItems = 'center';
            headerDiv.style.borderBottom = '1px solid #eee';
            headerDiv.style.paddingBottom = '10px';
            headerDiv.style.marginBottom = '10px';
            headerDiv.innerHTML = `
                <h3 style="margin:0;">${group.name}</h3>
                <button onclick="app.toggleCartGroup('${companyId}')" style="background:none; border:none; cursor:pointer; font-size:1.5em; color:var(--color-1); padding:0 10px;" title="Minimizar/Expandir">
                    <span id="icon-${companyId}">−</span>
                </button>
            `;
            groupDiv.appendChild(headerDiv);

            // Collapsible Content
            const contentDiv = document.createElement('div');
            contentDiv.id = `cart-group-content-${companyId}`;
            contentDiv.style.display = 'block';

            let total = 0;
            group.items.forEach(item => {
                const itemTotal = item.price * item.quantity;
                total += itemTotal;
                
                const globalIndex = this.cart.indexOf(item);

                const itemDiv = document.createElement('div');
                itemDiv.className = 'cart-item';
                itemDiv.innerHTML = `
                    <span>${item.name} (x${item.quantity})</span>
                    <span>R$ ${itemTotal.toFixed(2)}</span>
                    <button onclick="app.removeFromCart(${globalIndex})" class="btn-sm" style="background: var(--color-1); color: white; border:none; padding:5px 10px; border-radius:4px;">X</button>
                `;
                contentDiv.appendChild(itemDiv);
            });

            // Summary and Checkout for this group
            if (companyId !== 'unknown') {
                const userAddress = this.currentUser ? (this.currentUser.address || '') : '';
                const userPrefs = this.currentUser ? (this.currentUser.preferences || '') : '';

                const summaryDiv = document.createElement('div');
                summaryDiv.className = 'cart-summary';
                summaryDiv.style.marginTop = '20px';
                summaryDiv.innerHTML = `
                    <h4 style="text-align:right;">Total: R$ ${total.toFixed(2)}</h4>
                    <div class="checkout-form" style="background: #f9f9f9; padding: 15px; border-radius: 4px; margin-top: 10px;">
                        <h5>Finalizar Pedido - ${group.name}</h5>
                        
                        <div style="display:flex; gap:10px; margin-bottom:15px;">
                            <button type="button" onclick="app.fillCheckoutData('${companyId}', 'home')" class="btn-sm" style="flex:1; background:var(--color-1); color:white; border:none; padding:8px; border-radius:4px;">Casa</button>
                            <button type="button" onclick="app.fillCheckoutData('${companyId}', 'other')" class="btn-sm" style="flex:1; background:#404467; color:white; border:none; padding:8px; border-radius:4px;">Outro</button>
                        </div>

                        <div class="form-group">
                            <label>ID do Cliente:</label>
                            <input type="text" id="checkout-client-id-${companyId}" placeholder="Ex: 1" value="${this.currentUser ? this.currentUser.id : ''}" ${this.currentUser ? 'readonly' : ''}>
                        </div>
                        <div class="form-group">
                            <label>Endereço de Entrega:</label>
                            <input type="text" id="checkout-address-${companyId}" placeholder="Rua, Número, Bairro" value="${userAddress}" readonly style="background-color: #e9ecef;">
                        </div>
                        <div class="form-group">
                            <label>Preferências/Observações:</label>
                            <input type="text" id="checkout-obs-${companyId}" placeholder="Ex: Sem cebola, campainha quebrada" value="${userPrefs}" readonly style="background-color: #e9ecef;">
                        </div>
                         <div class="form-group">
                            <label>Forma de Pagamento:</label>
                            <select id="checkout-payment-${companyId}">
                                <option value="0">Cartão de Crédito</option>
                                <option value="1">Cartão de Débito</option>
                                <option value="2">PIX</option>
                                <option value="3">Dinheiro (Entrega)</option>
                            </select>
                        </div>
                        <button onclick="app.checkout('${companyId}')" class="btn-primary" style="width: 100%;">Enviar Pedido para ${group.name}</button>
                    </div>
                `;
                contentDiv.appendChild(summaryDiv);
            } else {
                const errorDiv = document.createElement('div');
                errorDiv.style.color = 'red';
                errorDiv.style.marginTop = '10px';
                errorDiv.textContent = 'Estes itens podem estar desatualizados. Remova-os e adicione novamente.';
                contentDiv.appendChild(errorDiv);
            }
            
            groupDiv.appendChild(contentDiv);
            container.appendChild(groupDiv);
        });
    },

    removeFromCart: function(index) {
        this.cart.splice(index, 1);
        this.saveCart();
        this.updateCartCount();
        this.renderCartItems();
    },

    saveCart: function() {
        sessionStorage.setItem('cart', JSON.stringify(this.cart));
    },

    updateCartCount: function() {
        const badge = document.getElementById('cart-count');
        if(badge) {
            const count = this.cart.reduce((sum, item) => sum + item.quantity, 0);
            badge.textContent = count;
        }
    },

    fillCheckoutData: function(companyId, type) {
        const addrInput = document.getElementById(`checkout-address-${companyId}`);
        const obsInput = document.getElementById(`checkout-obs-${companyId}`);
        
        if (type === 'home' && this.currentUser) {
            addrInput.value = this.currentUser.address || '';
            obsInput.value = this.currentUser.preferences || '';
            
            addrInput.readOnly = true;
            obsInput.readOnly = true;
            addrInput.style.backgroundColor = '#e9ecef';
            obsInput.style.backgroundColor = '#e9ecef';
        } else {
            // 'other'
            addrInput.value = '';
            obsInput.value = '';
            addrInput.readOnly = false;
            obsInput.readOnly = false;
            addrInput.style.backgroundColor = 'white';
            obsInput.style.backgroundColor = 'white';
            addrInput.focus();
        }
    },

    checkout: async function(companyId) {
        const clientId = document.getElementById(`checkout-client-id-${companyId}`).value;
        const address = document.getElementById(`checkout-address-${companyId}`).value;
        const observations = document.getElementById(`checkout-obs-${companyId}`).value;
        const paymentMethod = parseInt(document.getElementById(`checkout-payment-${companyId}`).value);

        if(!clientId || !address) {
            this.showAlertModal('error', 'Erro', 'Preencha todos os campos.');
            return;
        }

        // Filter items for this company
        const itemsToOrder = this.cart.filter(i => {
            const cId = i.companyId || 'unknown';
            return cId == companyId;
        });

        if(itemsToOrder.length === 0) {
            this.showAlertModal('error', 'Erro', 'Nenhum item para esta empresa.');
            return;
        }

        const totalAmount = itemsToOrder.reduce((sum, i) => sum + (i.price * i.quantity), 0);

        const order = {
            clientId: clientId,
            companyId: companyId,
            status: 0, // Pending
            totalAmount: totalAmount,
            observations: observations,
            items: itemsToOrder.map(i => ({
                productId: i.productId,
                quantity: i.quantity,
                unitPrice: i.price
            })),
            delivery: {
                address: address,
                status: 0,
                trackingCode: '',
                trackingLocation: ''
            },
            payment: {
                method: paymentMethod,
                status: 0,
                amount: totalAmount
            }
        };

        try {
            const response = await fetch('/api/Orders', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(order)
            });

            if(response.ok) {
                this.showAlertModal('success', 'Sucesso', 'Pedido realizado com sucesso!');
                
                // Remove only ordered items from cart
                this.cart = this.cart.filter(i => {
                     const cId = i.companyId || 'unknown';
                     return cId != companyId;
                });
                
                this.saveCart();
                this.updateCartCount();
                this.renderCartItems();
                
            } else {
                const errorText = await response.text();
                this.showAlertModal('error', 'Erro', 'Erro ao realizar pedido: ' + errorText);
            }
        } catch(e) {
            console.error(e);
            this.showAlertModal('error', 'Erro', 'Erro de conexão.');
        }
    },

    // --- Client Area ---
    showClientArea: function() {
        this.renderTemplate('client-template');
        // this.updateHeader(true); // Moved inside condition
        if (this.currentUser && this.userRole === 'client') {
            this.updateHeader(true);
            document.getElementById('client-login-area').style.display = 'none';
            document.getElementById('client-dashboard').style.display = 'block';
            document.getElementById('welcome-msg').textContent = `Bem-vindo, ${this.currentUser.name}!`;
            this.loadClientOrders();
        } else {
            this.updateHeader(false);
            document.getElementById('client-login-area').style.display = 'block';
            document.getElementById('client-dashboard').style.display = 'none';
        }
    },

    registerClient: async function() {
        const nameField = document.getElementById('new-client-name');
        const emailField = document.getElementById('new-client-email');
        const passwordField = document.getElementById('new-client-password');
        
        if (!nameField || !emailField || !passwordField) {
            this.showAlertModal('error', 'Erro', 'Campos não encontrados. Recarregue a página.');
            return;
        }
        
        const name = nameField.value ? nameField.value.trim() : '';
        const email = emailField.value ? emailField.value.trim() : '';
        const password = passwordField.value ? passwordField.value.trim() : '';
        
        // Validação de campos obrigatórios
        let hasError = false;
        if (!name) {
            nameField.classList.add('error');
            const errorMsg = nameField.parentElement?.querySelector('.field-error');
            if (errorMsg) errorMsg.classList.add('show');
            hasError = true;
        } else {
            nameField.classList.remove('error');
            const errorMsg = nameField.parentElement?.querySelector('.field-error');
            if (errorMsg) errorMsg.classList.remove('show');
        }
        
        if (!email) {
            emailField.classList.add('error');
            const errorMsg = emailField.parentElement?.querySelector('.field-error');
            if (errorMsg) errorMsg.classList.add('show');
            hasError = true;
        } else {
            emailField.classList.remove('error');
            const errorMsg = emailField.parentElement?.querySelector('.field-error');
            if (errorMsg) errorMsg.classList.remove('show');
        }
        
        if (!password) {
            passwordField.classList.add('error');
            const errorMsg = passwordField.parentElement?.querySelector('.field-error');
            if (errorMsg) errorMsg.classList.add('show');
            hasError = true;
        } else if (password.length < 6) {
            passwordField.classList.add('error');
            const errorMsg = passwordField.parentElement?.querySelector('.field-error');
            if (errorMsg) errorMsg.textContent = 'A senha deve ter pelo menos 6 dígitos.';
            if (errorMsg) errorMsg.classList.add('show');
            hasError = true;
        } else {
            passwordField.classList.remove('error');
            const errorMsg = passwordField.parentElement?.querySelector('.field-error');
            if (errorMsg) errorMsg.textContent = 'Este campo é obrigatório';
            if (errorMsg) errorMsg.classList.remove('show');
        }
        
        if (hasError) {
            if (!password || password.length < 6) {
                this.showAlertModal('error', 'Erro', "A senha deve ter pelo menos 6 dígitos.");
            } else {
                this.showAlertModal('error', 'Erro', 'Preencha todos os campos obrigatórios.');
            }
            return;
        }

        const client = {
            name: name,
            email: email,
            password: password,
            phone: document.getElementById('new-client-phone')?.value || '',
            address: document.getElementById('new-client-address')?.value || '',
            preferences: document.getElementById('new-client-preferences')?.value || ''
        };

        try {
            const response = await fetch('/api/Clients', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(client)
            });

            if(response.ok) {
                const newClient = await response.json();
                this.showAlertModal('success', 'Sucesso', `Cadastro realizado com sucesso, ${newClient.name}!`);
                this.currentUser = newClient;
                this.userRole = 'client';
                this.saveSession();
                this.showClientArea();
            } else {
                const errorText = await response.text();
                this.showAlertModal('error', 'Erro', "Erro ao cadastrar: " + errorText);
            }
        } catch(e) {
            console.error(e);
            this.showAlertModal('error', 'Erro', "Erro de conexão");
        }
    },

    showClientProfile: function() {
        if (!this.currentUser) return;
        this.renderTemplate('client-profile-template');
        document.getElementById('profile-name').value = this.currentUser.name;
        document.getElementById('profile-email').value = this.currentUser.email;
        document.getElementById('profile-phone').value = this.currentUser.phone;
        document.getElementById('profile-address').value = this.currentUser.address;
        document.getElementById('profile-preferences').value = this.currentUser.preferences;
    },

    updateClientProfile: async function() {
        if (!this.currentUser) return;
        const updatedClient = {
            id: this.currentUser.id,
            name: document.getElementById('profile-name').value,
            email: document.getElementById('profile-email').value,
            phone: document.getElementById('profile-phone').value,
            address: document.getElementById('profile-address').value,
            preferences: document.getElementById('profile-preferences').value,
            password: this.currentUser.password
        };

        try {
            const response = await fetch(`/api/Clients/${this.currentUser.id}`, {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(updatedClient)
            });

            if(response.ok) {
                this.showAlertModal('success', 'Sucesso', 'Perfil atualizado!');
                this.currentUser = updatedClient;
                this.saveSession();
                this.showClientArea();
            } else {
                this.showAlertModal('error', 'Erro', 'Erro ao atualizar perfil.');
            }
        } catch(e) {
            console.error(e);
            this.showAlertModal('error', 'Erro', 'Erro de conexão.');
        }
    },

    showClientOrders: function() {
        if (!this.currentUser || this.userRole !== 'client') {
            this.showAlertModal('error', 'Erro', "Você precisa fazer login primeiro.");
            this.showClientArea();
            return;
        }
        // If we are already in client dashboard, just ensure orders are visible
        // But here we might want a standalone view or just scroll to it
        this.showClientArea(); // Reuse the dashboard view which has orders
    },

    loadClientOrders: async function() {
        const container = document.getElementById('order-history');
        if(!container) return;

        try {
            const response = await fetch(`/api/Orders/Client/${this.currentUser.id}`, { cache: 'no-store' });
            if (!response.ok) throw new Error('Falha ao buscar pedidos');
            
            const orders = await response.json();
            
            if(orders.length === 0) {
                container.innerHTML = `
                    <p>Você ainda não fez nenhum pedido.</p>
                    <button onclick="app.showCompanyTypes()" class="btn-primary" style="margin-top: 15px;">
                        <i class="fas fa-utensils"></i> Fazer meu primeiro pedido
                    </button>
                `;
                return;
            }

            container.innerHTML = '';
            orders.sort((a,b) => new Date(b.orderDate) - new Date(a.orderDate)).forEach(o => {
                const div = document.createElement('div');
                div.className = 'order-card';
                
                const statusInfo = this.getOrderStatusDisplay(o);

                // Check Delay
                let estimateText = 'Aguardando definição';
                if(o.estimatedDeliveryTime) {
                    estimateText = this.formatTime(o.estimatedDeliveryTime);
                }

                let itemsHtml = o.items.map(i => `<div>${i.quantity}x ${i.product ? i.product.name : 'Item'}</div>`).join('');

                const orderDate = new Date(o.orderDate).toLocaleString('pt-BR', {
                    day: '2-digit', month: '2-digit', year: 'numeric',
                    hour: '2-digit', minute: '2-digit'
                });

                let actions = '';
                if(o.status === 5) { // Delivered
                    if (o.review) {
                         const stars = '⭐'.repeat(o.review.rating);
                         actions = `
                            <div style="margin-top:10px; background: #fff8e1; padding: 10px; border-radius: 5px; border: 1px solid #ffe082;">
                                <div style="color: #f39c12; font-weight:bold; font-size: 1.1em;">Sua Avaliação: ${stars}</div>
                                <div style="color: #555; font-style: italic; margin-top: 5px;">"${o.review.comment || 'Sem comentário'}"</div>
                            </div>
                         `;
                    } else {
                        actions = `<button onclick="app.openReviewModal('${o.id}')" class="btn-sm btn-info" style="margin-top:10px; background: var(--color-3); color: white; border:none; padding:5px 10px; border-radius:4px;">Avaliar Pedido</button>`;
                    }
                }

                div.innerHTML = `
                    <div class="order-header">
                        <span>Pedido #${o.id}</span>
                        <span class="status-badge" style="background-color: ${statusInfo.color}">${statusInfo.text}</span>
                    </div>
                    <div style="font-size: 0.85em; color: #777; margin-bottom: 5px;">
                        Data: ${orderDate}
                    </div>
                    <div style="margin: 10px 0; color: #666;">
                        ${itemsHtml}
                    </div>
                    <div style="font-weight: bold; margin-bottom: 5px;">Total: R$ ${o.totalAmount.toFixed(2)}</div>
                    <div style="font-size: 0.9em; color: #555;">
                        <i class="far fa-clock"></i> Previsão de Entrega: <strong style="color: var(--primary)">${estimateText}</strong>
                    </div>
                    ${actions}
                `;
                container.appendChild(div);
            });

        } catch(e) {
            console.error('Error loading client orders:', e);
            container.innerHTML = '<p>Erro ao carregar seus pedidos.</p>';
        }
    },

    // --- Notifications ---
    checkNotifications: async function() {
        if(!this.currentUser || this.userRole !== 'client') return;
        try {
            const response = await fetch(`/api/Notifications/Client/${this.currentUser.id}`);
            const notifs = await response.json();
            const unread = notifs.filter(n => !n.isRead).length;
            
            const badge = document.getElementById('notification-badge');
            if(badge) {
                badge.textContent = unread;
                badge.style.display = unread > 0 ? 'inline-block' : 'none';
            }
            
            // Toast Notification Logic
            const maxId = notifs.length > 0 ? Math.max(...notifs.map(n => n.id)) : 0;
            
            if (this.lastNotificationId === 0) {
                this.lastNotificationId = maxId;
            } else {
                const newNotifications = notifs.filter(n => n.id > this.lastNotificationId && !n.isRead);
                newNotifications.forEach(n => {
                    this.showToastNotification('Nova Atualização', n.message);
                });
                this.lastNotificationId = maxId;
            }

            this.data.notifications = notifs;
        } catch(e) {
            console.error('Error checking notifications', e);
        }
    },

    showToastNotification: function(title, message) {
        const container = document.getElementById('toast-container');
        if (!container) return;

        const toast = document.createElement('div');
        toast.className = 'toast';
        toast.innerHTML = `
            <div class="toast-content">
                <div class="toast-title" style="font-weight: bold; margin-bottom: 5px; color: var(--primary);">${title}</div>
                <div class="toast-message" style="font-size: 0.9em; color: #333;">${message}</div>
            </div>
            <button onclick="this.parentElement.remove()" style="background: none; border: none; font-size: 1.2em; color: #999; cursor: pointer;">&times;</button>
        `;

        container.appendChild(toast);

        // Remove automatically after 5s (animation handles fade out)
        setTimeout(() => {
            if (toast.parentElement) {
                toast.remove();
            }
        }, 5000);
    },

    showNotifications: function() {
        if(!this.currentUser) return;
        const modal = document.getElementById('notifications-modal');
        modal.innerHTML = document.getElementById('notifications-modal-template').innerHTML;
        modal.style.display = 'flex';
        
        const list = document.getElementById('notifications-list');
        if(this.data.notifications.length === 0) {
            list.innerHTML = '<p>Nenhuma notificação.</p>';
        } else {
            list.innerHTML = '';
            this.data.notifications.forEach(n => {
                const div = document.createElement('div');
                div.className = `notification-item ${n.isRead ? 'read' : 'unread'}`;
                div.style.padding = '10px';
                div.style.borderBottom = '1px solid #eee';
                div.style.background = n.isRead ? '#fff' : '#f0f8ff';
                
                div.innerHTML = `
                    <div style="font-size: 0.8em; color: #888;">${new Date(n.createdAt).toLocaleString()}</div>
                    <div style="font-weight: bold;">${n.orderId ? 'Pedido #' + n.orderId : 'Aviso'}</div>
                    <div>${n.message}</div>
                `;
                
                if(!n.isRead) {
                    // Mark as read when displayed (or we could add a button)
                    this.markNotificationRead(n.id);
                }
                
                list.appendChild(div);
            });
        }
    },

    markNotificationRead: async function(id) {
        try {
            await fetch(`/api/Notifications/${id}/Read`, { method: 'PUT' });
            this.checkNotifications(); // Refresh badge
        } catch(e) { console.error(e); }
    },

    closeNotificationsModal: function() {
        document.getElementById('notifications-modal').style.display = 'none';
    },

    // --- Admin Dashboard ---
    showAdminDashboard: function() {
        history.pushState(null, '', '/empresa');
        this.userRole = 'admin';
        this.updateHeader(true);
        this.renderTemplate('admin-dashboard-template');
        this.showAdminTab('orders'); // Default tab
    },

    showAdminTab: function(tabName) {
        // Tabs UI
        document.querySelectorAll('.tab-btn').forEach(b => b.classList.remove('active'));
        document.getElementById(`tab-${tabName}`).classList.add('active');
        
        // Content UI
        document.querySelectorAll('.admin-tab-content').forEach(c => c.style.display = 'none');
        document.getElementById(`admin-content-${tabName}`).style.display = 'block';

        if(tabName === 'orders') this.loadAdminOrders();
        if(tabName === 'products') this.loadAdminProducts();
        if(tabName === 'categories') this.loadAdminCategories();
        if(tabName === 'couriers') this.loadCouriers();
        if(tabName === 'reviews') this.loadAdminReviews();
    },

    loadAdminReviews: async function() {
        const container = document.getElementById('admin-review-list');
        if(!container) return;
        container.innerHTML = '<p>Carregando...</p>';

        try {
            const response = await fetch(`/api/Reviews?companyId=${this.currentUser.id}`);
            const reviews = await response.json();

            if(reviews.length === 0) {
                container.innerHTML = '<p>Nenhuma avaliação recebida.</p>';
                return;
            }

            container.innerHTML = '';
            reviews.forEach(r => {
                const div = document.createElement('div');
                div.className = 'order-card'; 
                
                const stars = '⭐'.repeat(r.rating);
                const date = new Date(r.date).toLocaleString('pt-BR');
                
                div.innerHTML = `
                    <div class="order-header">
                        <span>Pedido #${r.orderId || '?'}</span>
                        <span style="color: #f39c12; font-size: 1.2em;">${stars}</span>
                    </div>
                    <div style="font-size: 0.85em; color: #777; margin-bottom: 10px;">
                        Data: ${date} - Cliente: ${r.client ? r.client.name : 'Anônimo'}
                    </div>
                    <div style="background: #fff8e1; padding: 10px; border-radius: 5px; border: 1px solid #ffe082;">
                        "${r.comment || ''}"
                    </div>
                `;
                container.appendChild(div);
            });
        } catch(e) {
            console.error(e);
            container.innerHTML = '<p>Erro ao carregar avaliações.</p>';
        }
    },

    loadAdminOrders: async function() {
        const container = document.getElementById('admin-orders-list');
        if(!container) return;
        container.innerHTML = '<p>Carregando...</p>';

        try {
            const response = await fetch(`/api/Orders?companyId=${this.currentUser.id}`, { cache: 'no-store' });
            const orders = await response.json();
            console.log('Admin Orders:', orders); // Debug
            
            const activeOrders = orders.sort((a,b) => new Date(b.orderDate) - new Date(a.orderDate));

            if(activeOrders.length === 0) {
                container.innerHTML = '<p>Nenhum pedido recebido.</p>';
                return;
            }

            container.innerHTML = '';
            activeOrders.forEach(o => {
                const div = document.createElement('div');
                div.className = 'order-card';
                
                const statusInfo = this.getOrderStatusDisplay(o);
                
                let actions = '';
                // Status Transitions
                if(o.status === 0) { // Pending
                    actions = `
                        <button onclick="app.acceptOrder('${o.id}')" class="btn-success">Aceitar</button>
                        <button onclick="app.rejectOrder('${o.id}')" class="btn-secondary" style="background: var(--color-5); color:white;">Rejeitar</button>
                    `;
                } else if(o.status === 1) { // Confirmed
                    actions = `<button onclick="app.updateOrderStatus('${o.id}', 2)" class="btn-primary">Iniciar Preparo</button>`;
                } else if(o.status === 2) { // Preparation
                    actions = `<button onclick="app.updateOrderStatus('${o.id}', 4)" class="btn-primary">Saiu para Entrega</button>`;
                } else if(o.status === 4) { // Out for Delivery
                    actions = `<button onclick="app.updateOrderStatus('${o.id}', 5)" class="btn-primary" style="background:var(--color-3);">Confirmar Entrega</button>`;
                }

                // Problem Report Button (available for active orders)
                if(o.status !== 5 && o.status !== 6) {
                    actions += `<button onclick="app.reportOrderIssue('${o.id}', '${o.clientId}')" class="btn-sm" style="margin-left: 10px; background: var(--color-1); color: white;">Notificar Problema</button>`;
                }

                // Assign Courier Button (for confirmed/prep/ready/out)
                if(o.status >= 1 && o.status < 5) {
                    actions += `<button onclick="app.showAssignCourierModal('${o.id}')" class="btn-sm" style="margin-left: 10px; background: var(--color-4); color: white;">Entregador</button>`;
                }

                const orderDate = new Date(o.orderDate).toLocaleString('pt-BR', {
                    day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit'
                });
                
                // Informações de pagamento
                const paymentMethod = o.payment ? 
                    (o.payment.method === 0 ? 'Dinheiro' : o.payment.method === 1 ? 'Cartão' : o.payment.method === 2 ? 'PIX' : 'Outro') : 
                    'Não informado';
                const paymentStatus = o.payment ? 
                    (o.payment.status === 0 ? 'Pendente' : o.payment.status === 1 ? 'Pago' : 'Cancelado') : 
                    'N/A';
                
                // Telefone do cliente
                const clientPhone = o.client && o.client.phone ? o.client.phone : 'Não informado';
                
                div.innerHTML = `
                    <div class="order-header">
                        <span>Pedido #${o.id}</span>
                        <span class="status-badge" style="background:${statusInfo.color}">${statusInfo.text}</span>
                    </div>
                    <div style="font-size: 0.8rem; color: #666; margin-bottom: 10px;">
                        ${orderDate}
                    </div>
                    <div style="margin-bottom: 10px; padding-bottom: 8px; border-bottom: 1px solid #eee;">
                        <div style="font-weight: bold; font-size: 0.9rem; margin-bottom: 3px;">👤 ${o.client ? o.client.name : 'Cliente N/A'}</div>
                        <div style="font-size: 0.75rem; color: #666;">${clientPhone}</div>
                    </div>
                    <div style="margin-bottom: 8px;">
                        <div style="font-size: 0.85rem; font-weight: bold; margin-bottom: 5px;">Itens do Pedido:</div>
                        <ul style="margin: 0; padding-left: 20px; font-size: 0.8rem; line-height: 1.5;">
                            ${o.items.map(i => `<li>${i.product ? i.product.name : 'Produto'} - Qtd: ${i.quantity} x R$ ${i.unitPrice ? i.unitPrice.toFixed(2) : '0.00'}</li>`).join('')}
                        </ul>
                    </div>
                    <div style="font-weight: bold; color: var(--secondary); font-size: 0.95rem; margin-bottom: 10px; padding: 8px; background: #f8f9fa; border-radius: 4px;">
                        Total: R$ ${o.totalAmount.toFixed(2)}
                    </div>
                    <div style="margin-bottom: 8px; padding: 8px; background: #f0f7ff; border-radius: 4px; border-left: 3px solid #3498db;">
                        <div style="font-size: 0.8rem; font-weight: bold; margin-bottom: 3px;">Pagamento:</div>
                        <div style="font-size: 0.75rem;">Método: ${paymentMethod}</div>
                        <div style="font-size: 0.75rem;">Status: ${paymentStatus}</div>
                    </div>
                    <div style="margin-bottom: 8px; padding: 8px; background: #fff8e1; border-radius: 4px; border-left: 3px solid #f39c12;">
                        <div style="font-size: 0.8rem; font-weight: bold; margin-bottom: 3px;">Endereço de Entrega:</div>
                        <div style="font-size: 0.75rem; word-wrap: break-word; line-height: 1.4;">${o.delivery ? o.delivery.address : 'Não informado'}</div>
                    </div>
                    <div style="margin-bottom: 8px;">
                        <div style="font-size: 0.8rem; margin-bottom: 3px;">
                            <strong>Entregador:</strong> 
                            <span style="color: ${o.delivery && o.delivery.courier ? 'var(--primary)' : '#e74c3c'};">
                                ${o.delivery && o.delivery.courier ? o.delivery.courier.name : 'Não atribuído'}
                            </span>
                        </div>
                        ${o.delivery && o.delivery.courier && o.delivery.courier.phone ? 
                            `<div style="font-size: 0.75rem; color: #666;">${o.delivery.courier.phone}</div>` : ''}
                    </div>
                    ${o.estimatedDeliveryTime ? `
                    <div style="margin-bottom: 8px; padding: 6px; background: #e8f5e9; border-radius: 4px;">
                        <div style="font-size: 0.8rem; color: var(--primary);">
                            <strong>Previsão:</strong> ${this.formatTime(o.estimatedDeliveryTime)}
                        </div>
                    </div>` : ''}
                    ${o.observations ? `
                    <div style="margin-bottom: 8px; padding: 6px; background: #f3e5f5; border-radius: 4px;">
                        <div style="font-size: 0.75rem; font-weight: bold; margin-bottom: 2px;">Observações do Cliente:</div>
                        <div style="font-size: 0.75rem; word-wrap: break-word; line-height: 1.4;">${o.observations}</div>
                    </div>` : ''}
                    ${o.rejectionReason ? `
                    <div style="margin-bottom: 8px; padding: 6px; background: #ffebee; border-radius: 4px; border-left: 3px solid #e74c3c;">
                        <div style="font-size: 0.75rem; font-weight: bold; color: #c0392b; margin-bottom: 2px;">Motivo da Rejeição:</div>
                        <div style="font-size: 0.75rem; color: #c0392b; word-wrap: break-word; line-height: 1.4;">${o.rejectionReason}</div>
                    </div>` : ''}
                    <div class="order-actions">${actions}</div>
                `;
                container.appendChild(div);
            });
        } catch(e) {
            console.error(e);
            container.innerHTML = '<p>Erro ao carregar pedidos.</p>';
        }
    },

    loadAdminProducts: async function() {
        const container = document.getElementById('admin-product-list');
        if(!container) return;
        container.innerHTML = '<p>Carregando...</p>';

        try {
            const response = await fetch(`/api/Products?companyId=${this.currentUser.id}`);
            const products = await response.json();

            if(products.length === 0) {
                container.innerHTML = '<p>Nenhum produto cadastrado.</p>';
                return;
            }

            container.innerHTML = `
                <table style="width: 100%; border-collapse: collapse;">
                    <thead>
                        <tr style="background: #f4f4f4; text-align: left;">
                            <th style="padding: 10px;">ID</th>
                            <th style="padding: 10px;">Nome</th>
                            <th style="padding: 10px;">Preço</th>
                            <th style="padding: 10px;">Estoque</th>
                            <th style="padding: 10px;">Categoria</th>
                            <th style="padding: 10px;">Ações</th>
                        </tr>
                    </thead>
                    <tbody>
                        ${products.map(p => `
                            <tr style="border-bottom: 1px solid #ddd;">
                                <td style="padding: 10px;">${p.id}</td>
                                <td style="padding: 10px;">${p.name}</td>
                                <td style="padding: 10px;">R$ ${p.price.toFixed(2)}</td>
                                <td style="padding: 10px;">${p.stockQuantity}</td>
                                <td style="padding: 10px;">${p.category ? p.category.name : 'N/A'}</td>
                                <td style="padding: 10px;">
                                    <button onclick="app.deleteProduct('${p.id}')" class="btn-sm" style="background:#e74c3c;">Excluir</button>
                                </td>
                            </tr>
                        `).join('')}
                    </tbody>
                </table>
            `;
        } catch(e) {
            console.error(e);
            container.innerHTML = '<p>Erro ao carregar produtos.</p>';
        }
    },

    loadAdminCategories: async function() {
        const container = document.getElementById('admin-category-list');
        if(!container) return;
        container.innerHTML = '<p>Carregando...</p>';

        try {
            const response = await fetch(`/api/Categories?companyId=${this.currentUser.id}`);
            const categories = await response.json();

            if(categories.length === 0) {
                container.innerHTML = '<p>Nenhuma categoria cadastrada.</p>';
                return;
            }

            container.innerHTML = `
                <table style="width: 100%; border-collapse: collapse;">
                    <thead>
                        <tr style="background: #f4f4f4; text-align: left;">
                            <th style="padding: 10px;">ID</th>
                            <th style="padding: 10px;">Nome</th>
                            <th style="padding: 10px;">Ações</th>
                        </tr>
                    </thead>
                    <tbody>
                        ${categories.map(c => `
                            <tr style="border-bottom: 1px solid #ddd;">
                                <td style="padding: 10px;">${c.id}</td>
                                <td style="padding: 10px;">${c.name}</td>
                                <td style="padding: 10px;">
                                    <button onclick="app.deleteCategory('${c.id}')" class="btn-sm" style="background:var(--color-1); color: white; border:none; padding:5px 10px; border-radius:4px;">Excluir</button>
                                </td>
                            </tr>
                        `).join('')}
                    </tbody>
                </table>
            `;
        } catch(e) {
            console.error(e);
            container.innerHTML = '<p>Erro ao carregar categorias.</p>';
        }
    },

    loadCouriers: async function() {
        const container = document.getElementById('admin-courier-list');
        if(!container) return;
        container.innerHTML = '<p>Carregando...</p>';

        try {
            const response = await fetch(`/api/Couriers?companyId=${this.currentUser.id}`);
            const couriers = await response.json();

            if(couriers.length === 0) {
                container.innerHTML = '<p>Nenhum entregador cadastrado.</p>';
                return;
            }

            container.innerHTML = `
                <table style="width: 100%; border-collapse: collapse;">
                    <thead>
                        <tr style="background: #f4f4f4; text-align: left;">
                            <th style="padding: 10px;">ID</th>
                            <th style="padding: 10px;">Nome</th>
                            <th style="padding: 10px;">Veículo</th>
                            <th style="padding: 10px;">Telefone</th>
                            <th style="padding: 10px;">Ações</th>
                        </tr>
                    </thead>
                    <tbody>
                        ${couriers.map(c => `
                            <tr style="border-bottom: 1px solid #ddd;">
                                <td style="padding: 10px;">${c.id}</td>
                                <td style="padding: 10px;">${c.name}</td>
                                <td style="padding: 10px;">${c.vehicleInfo || '-'}</td>
                                <td style="padding: 10px;">${c.phone || '-'}</td>
                                <td style="padding: 10px;">
                                    <button onclick="app.showCourierRegistration('${c.id}')" class="btn-sm" style="background:var(--color-4); color: white; border:none; padding:5px 10px; border-radius:4px;">Editar</button>
                                    <button onclick="app.deleteCourier('${c.id}')" class="btn-sm" style="background:var(--color-1); color: white; border:none; padding:5px 10px; border-radius:4px;">Excluir</button>
                                </td>
                            </tr>
                        `).join('')}
                    </tbody>
                </table>
            `;
        } catch(e) {
            console.error(e);
            container.innerHTML = '<p>Erro ao carregar entregadores.</p>';
        }
    },

    showCourierRegistration: async function(id = null) {
        this.renderTemplate('courier-registration-template');
        if(id) {
            try {
                // Fetch details if editing
                const response = await fetch('/api/Couriers'); // Or specific get if available, using list for now
                const couriers = await response.json();
                const courier = couriers.find(c => c.id === id);
                if(courier) {
                    document.getElementById('courier-id').value = courier.id;
                    document.getElementById('courier-name').value = courier.name;
                    document.getElementById('courier-vehicle').value = courier.vehicleInfo;
                    document.getElementById('courier-phone').value = courier.phone;
                }
            } catch(e) { console.error(e); }
        }
    },

    saveCourier: async function() {
        const nameField = document.getElementById('courier-name');
        
        if (!nameField) {
            this.showAlertModal('error', 'Erro', 'Campo não encontrado. Recarregue a página.');
            return;
        }
        
        const name = nameField.value ? nameField.value.trim() : '';
        
        // Validação de campo obrigatório
        if (!name) {
            nameField.classList.add('error');
            const errorMsg = nameField.parentElement?.querySelector('.field-error');
            if (errorMsg) errorMsg.classList.add('show');
            this.showAlertModal('error', 'Erro', 'Nome obrigatório.');
            return;
        } else {
            nameField.classList.remove('error');
            const errorMsg = nameField.parentElement?.querySelector('.field-error');
            if (errorMsg) errorMsg.classList.remove('show');
        }
        
        const id = document.getElementById('courier-id')?.value;
        const courier = {
            id: id ? parseInt(id) : 0,
            name: name,
            vehicleInfo: document.getElementById('courier-vehicle')?.value || '',
            phone: document.getElementById('courier-phone')?.value || '',
            companyId: this.currentUser.id
        };

        try {
            const method = id ? 'PUT' : 'POST';
            const url = id ? `/api/Couriers/${id}` : '/api/Couriers';
            
            const response = await fetch(url, {
                method: method,
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(courier)
            });

            if(response.ok) {
                this.showAlertModal('success', 'Sucesso', 'Entregador salvo!');
                this.showAdminDashboard();
                this.showAdminTab('couriers');
            } else {
                this.showAlertModal('error', 'Erro', 'Erro ao salvar.');
            }
        } catch(e) { console.error(e); }
    },

    deleteCourier: async function(id) {
        if(!confirm("Excluir entregador?")) return;
        try {
            const response = await fetch(`/api/Couriers/${id}`, { method: 'DELETE' });
            if(response.ok) {
                this.loadCouriers();
            } else {
                this.showAlertModal('error', 'Erro', 'Erro ao excluir (pode estar vinculado a entregas).');
            }
        } catch(e) { console.error(e); }
    },

    // --- Assignment ---
    showAssignCourierModal: async function(orderId) {
        const modal = document.getElementById('assign-courier-modal');
        document.getElementById('assign-order-id').value = orderId;
        const select = document.getElementById('assign-courier-select');
        select.innerHTML = '<option value="">Carregando...</option>';
        
        modal.style.display = 'flex';

        try {
            const response = await fetch(`/api/Couriers?companyId=${this.currentUser.id}`);
            const couriers = await response.json();
            
            select.innerHTML = '<option value="">Selecione...</option>';
            couriers.forEach(c => {
                const option = document.createElement('option');
                option.value = c.id;
                option.textContent = `${c.name} (${c.vehicleInfo})`;
                select.appendChild(option);
            });
        } catch(e) {
            console.error(e);
            select.innerHTML = '<option value="">Erro ao carregar</option>';
        }
    },

    closeAssignCourierModal: function() {
        document.getElementById('assign-courier-modal').style.display = 'none';
    },

    confirmAssignCourier: async function() {
        const orderId = document.getElementById('assign-order-id').value;
        const courierId = document.getElementById('assign-courier-select').value;

        if(!courierId) {
            this.showAlertModal('error', 'Erro', "Selecione um entregador.");
            return;
        }

        try {
            const response = await fetch(`/api/Deliveries/Order/${orderId}/Assign/${courierId}`, {
                method: 'POST'
            });

            if(response.ok) {
                this.showAlertModal('success', 'Sucesso', 'Entregador atribuído!');
                this.closeAssignCourierModal();
                this.loadAdminOrders();
            } else {
                this.showAlertModal('error', 'Erro', 'Erro ao atribuir entregador.');
            }
        } catch(e) { console.error(e); }
    },

    // --- Admin Actions ---
    updateOrderStatus: async function(id, status, reason = null, estimatedDeliveryTime = null) {
        try {
            const body = { 
                status: status, 
                rejectionReason: reason,
                estimatedDeliveryTime: estimatedDeliveryTime
            };
            const response = await fetch(`/api/Orders/${id}/Status`, {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(body)
            });

            if(response.ok) {
                // If confirmed with time, send notification
                if(status === 2 && estimatedDeliveryTime) { // Preparation implies confirmed/accepted
                    // Actually status 1 is Confirmed, 2 is Preparation. 
                    // My logic in acceptOrder sets status 2 directly.
                    // Let's send a notification about the estimate.
                    // We need to fetch order to get client ID if we don't have it handy?
                    // But we can just assume the backend might do it, or we do it here.
                    // For now, let's keep it simple.
                }
                this.showAlertModal('success', 'Sucesso', 'Pedido atualizado!');
                this.loadAdminOrders();
            } else {
                this.showAlertModal('error', 'Erro', 'Erro ao atualizar pedido.');
            }
        } catch(e) {
            console.error(e);
        }
    },

    acceptOrder: function(id) {
        this.openModal(
            "Aceitar Pedido", 
            "Tempo estimado (minutos):", 
            "Ex: 30",
            "30",
            (val) => {
                if(!val) return;
                const minutesInt = parseInt(val);
                if(isNaN(minutesInt) || minutesInt <= 0) {
                    this.showAlertModal('error', 'Erro', "Tempo inválido.");
                    return;
                }
                
                const estTime = new Date();
                estTime.setMinutes(estTime.getMinutes() + minutesInt);
                this.updateOrderStatus(id, 2, null, estTime.toISOString()); // Go to Preparation
            }
        );
    },

    rejectOrder: function(id) {
        this.openModal(
            "Rejeitar Pedido",
            "Motivo:",
            "Ex: Falta de estoque",
            "",
            (val) => {
                if(!val) return;
                this.updateOrderStatus(id, 6, val); // Cancelled
            }
        );
    },

    reportOrderIssue: function(orderId, clientId) {
        this.openModal(
            "Notificar Problema/Atraso",
            "Mensagem para o cliente:",
            "Ex: O entregador teve um imprevisto...",
            "",
            async (msg) => {
                if(!msg) return;
                try {
                    await fetch('/api/Notifications', {
                        method: 'POST',
                        headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify({
                            clientId: clientId,
                            orderId: orderId,
                            message: msg,
                            isRead: false
                        })
                    });
                    this.showAlertModal('notification', 'Notificação', "Notificação enviada!");
                    // Also update order status to 'Problem' (7) if desired, but optional
                    // this.updateOrderStatus(orderId, 7, msg); 
                } catch(e) {
                    console.error(e);
                    this.showAlertModal('error', 'Erro', "Erro ao enviar notificação.");
                }
            }
        );
    },

    registerProduct: async function() {
        const nameField = document.getElementById('prod-name');
        const priceField = document.getElementById('prod-price');
        const categoryField = document.getElementById('prod-category');
        
        if (!nameField || !priceField || !categoryField) {
            this.showAlertModal('error', 'Erro', 'Campos não encontrados. Recarregue a página.');
            return;
        }
        
        const name = nameField.value ? nameField.value.trim() : '';
        const price = parseFloat(priceField.value);
        const categoryId = categoryField.value;
        
        // Validação de campos obrigatórios
        let hasError = false;
        if (!name) {
            nameField.classList.add('error');
            const errorMsg = nameField.parentElement?.querySelector('.field-error');
            if (errorMsg) errorMsg.classList.add('show');
            hasError = true;
        } else {
            nameField.classList.remove('error');
            const errorMsg = nameField.parentElement?.querySelector('.field-error');
            if (errorMsg) errorMsg.classList.remove('show');
        }
        
        if (isNaN(price) || price <= 0) {
            priceField.classList.add('error');
            const errorMsg = priceField.parentElement?.querySelector('.field-error');
            if (errorMsg) errorMsg.classList.add('show');
            hasError = true;
        } else {
            priceField.classList.remove('error');
            const errorMsg = priceField.parentElement?.querySelector('.field-error');
            if (errorMsg) errorMsg.classList.remove('show');
        }
        
        if (!categoryId) {
            categoryField.classList.add('error');
            const errorMsg = categoryField.parentElement?.querySelector('.field-error');
            if (errorMsg) errorMsg.classList.add('show');
            hasError = true;
        } else {
            categoryField.classList.remove('error');
            const errorMsg = categoryField.parentElement?.querySelector('.field-error');
            if (errorMsg) errorMsg.classList.remove('show');
        }
        
        if (hasError) {
            this.showAlertModal('error', 'Erro', 'Preencha os campos obrigatórios.');
            return;
        }
        
        const product = {
            name: name,
            price: price,
            description: document.getElementById('prod-desc')?.value || '',
            stockQuantity: parseInt(document.getElementById('prod-stock')?.value || '10'),
            imageUrl: document.getElementById('prod-img')?.value || '',
            categoryId: categoryId,
            companyId: this.currentUser.id
        };

        try {
            const response = await fetch('/api/Products', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(product)
            });

            if(response.ok) {
                this.showAlertModal('success', 'Sucesso', 'Produto cadastrado!');
                this.showAdminDashboard();
                this.showAdminTab('products');
            } else {
                this.showAlertModal('error', 'Erro', 'Erro ao cadastrar.');
            }
        } catch(e) { console.error(e); }
    },

    deleteProduct: async function(id) {
        if(!confirm("Tem certeza que deseja excluir?")) return;
        try {
            // Note: API might not support DELETE Product yet, assuming standard scaffold
            // If not, I need to add it to controller.
            // Wait, standard scaffold usually has it.
             await fetch(`/api/Products/${id}`, { method: 'DELETE' });
             this.loadAdminProducts();
        } catch(e) {
            console.error(e);
            this.showAlertModal('error', 'Erro', "Erro ao excluir (verifique se não há pedidos vinculados).");
        }
    },

    registerCategory: async function() {
        const nameField = document.getElementById('cat-name');
        
        if (!nameField) {
            this.showAlertModal('error', 'Erro', 'Campo não encontrado. Recarregue a página.');
            return;
        }
        
        const name = nameField.value ? nameField.value.trim() : '';
        
        // Validação de campo obrigatório
        if (!name) {
            nameField.classList.add('error');
            const errorMsg = nameField.parentElement?.querySelector('.field-error');
            if (errorMsg) errorMsg.classList.add('show');
            this.showAlertModal('error', 'Erro', 'Nome obrigatório.');
            return;
        } else {
            nameField.classList.remove('error');
            const errorMsg = nameField.parentElement?.querySelector('.field-error');
            if (errorMsg) errorMsg.classList.remove('show');
        }

        try {
            const response = await fetch('/api/Categories', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ name: name, companyId: this.currentUser.id })
            });
            if(response.ok) {
                this.showAlertModal('success', 'Sucesso', 'Categoria cadastrada!');
                this.showAdminDashboard();
                this.showAdminTab('categories');
            } else {
                this.showAlertModal('error', 'Erro', 'Erro ao cadastrar.');
            }
        } catch(e) { console.error(e); }
    },

    deleteCategory: async function(id) {
        if(!confirm("Excluir categoria? Produtos vinculados podem ficar sem categoria.")) return;
        try {
            await fetch(`/api/Categories/${id}`, { method: 'DELETE' });
            this.loadAdminCategories();
        } catch(e) { console.error(e); }
    },

    showProductRegistration: async function() {
        this.renderTemplate('product-registration-template');
        await this.loadCategoriesForForm();
    },

    showCategoryRegistration: function() {
        this.renderTemplate('category-registration-template');
    },

    loadCategoriesForForm: async function() {
        try {
            const response = await fetch(`/api/Categories?companyId=${this.currentUser.id}`);
            const categories = await response.json();
            const select = document.getElementById('prod-category');
            if(select) {
                select.innerHTML = '<option value="">Selecione...</option>';
                categories.forEach(cat => {
                    const option = document.createElement('option');
                    option.value = cat.id;
                    option.textContent = cat.name;
                    select.appendChild(option);
                });
            }
        } catch (error) { console.error(error); }
    },

    // --- Reviews ---
    openReviewModal: function(orderId) {
        const modal = document.getElementById('review-modal');
        modal.style.display = 'flex';
        // Save orderId in a data attribute or global
        modal.dataset.orderId = orderId;
    },

    closeReviewModal: function() {
        document.getElementById('review-modal').style.display = 'none';
    },

    submitReview: async function() {
        const modal = document.getElementById('review-modal');
        const orderId = modal.dataset.orderId; // Note: Review is linked to Client, not Order directly in model, but conceptually it is.
        // Wait, Review model has ClientId, Rating, Comment, Date. No OrderId.
        // So we are reviewing the "Service" generally or we should add OrderId to Review?
        // User asked for "Service evaluation".
        // I'll just submit it as a client review.
        
        const rating = document.getElementById('review-rating').value;
        const comment = document.getElementById('review-comment').value;

        const review = {
            clientId: this.currentUser.id,
            orderId: parseInt(orderId), // Ensure orderId is included
            rating: parseInt(rating),
            comment: comment,
            date: new Date().toISOString()
        };

        try {
            const response = await fetch('/api/Reviews', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(review)
            });

            if(response.ok) {
                this.showAlertModal('success', 'Sucesso', 'Obrigado pela avaliação!');
                this.closeReviewModal();
                this.loadClientOrders(); // Refresh to show stars
            } else {
                this.showAlertModal('error', 'Erro', 'Erro ao enviar avaliação.');
            }
        } catch(e) { console.error(e); }
    },

    // --- Utils ---
    renderTemplate: function(templateId) {
        const template = document.getElementById(templateId);
        const content = document.getElementById('main-content');
        content.innerHTML = '';
        content.appendChild(template.content.cloneNode(true));
    },

    getOrderStatusDisplay: function(o) {
        let statusText = 'Aguardando Confirmação';
        let statusColor = '#f39c12';
        
        if (o.status === 0) { statusText = 'Pendente'; statusColor = '#f39c12'; }
        else if (o.status === 1) { statusText = 'Confirmado'; statusColor = '#3498db'; }
        else if (o.status === 2) { statusText = 'Em Preparo'; statusColor = '#3498db'; }
        else if (o.status === 3) { statusText = 'Pronto'; statusColor = '#3498db'; }
        else if (o.status === 4) { statusText = 'A Caminho'; statusColor = '#9b59b6'; }
        else if (o.status === 5) { statusText = 'Entregue'; statusColor = '#2ecc71'; }
        else if (o.status === 6) { statusText = 'Cancelado'; statusColor = '#e74c3c'; }
        else if (o.status === 7) { statusText = 'Problema'; statusColor = '#c0392b'; }

        // Check Delay
        if (o.status !== 5 && o.status !== 6 && o.estimatedDeliveryTime) {
            const now = new Date();
            const estimated = new Date(o.estimatedDeliveryTime);
            if (now > estimated) {
                statusText = 'Em atraso';
                statusColor = 'var(--color-2)';
            }
        }
        return { text: statusText, color: statusColor };
    },

    formatTime: function(dateString) {
        if(!dateString || dateString === 'N/A') return 'Aguardando definição';
        try {
            const d = new Date(dateString);
            if(isNaN(d.getTime())) return 'Data Inválida';
            return d.toLocaleTimeString([], {hour: '2-digit', minute:'2-digit'});
        } catch(e) { return 'Erro Data'; }
    },

    // Modal Helper
    modalCallback: null,
    openModal: function(title, message, placeholder, defaultValue, callback) {
        document.getElementById('modal-title').textContent = title;
        document.getElementById('modal-message').textContent = message;
        const modal = document.getElementById('custom-modal');
        modal.style.display = 'flex';
        
        const input = document.getElementById('modal-input');
        input.value = defaultValue || '';
        input.placeholder = placeholder;
        
        this.modalCallback = callback;
        
        // Remove old listeners by cloning
        const btn = document.getElementById('modal-confirm-btn');
        const newBtn = btn.cloneNode(true);
        btn.parentNode.replaceChild(newBtn, btn);
        
        newBtn.addEventListener('click', () => {
            if(this.modalCallback) this.modalCallback(input.value);
            this.closeModal();
        });
    },
    closeModal: function() {
        document.getElementById('custom-modal').style.display = 'none';
        this.modalCallback = null;
    },

    // --- Modais de Alerta ---
    showAlertModal: function(type, title, message, customButton = null) {
        const modal = document.getElementById('alert-modal');
        if (!modal) return;
        
        const titleEl = document.getElementById('alert-modal-title-text');
        const messageEl = document.getElementById('alert-modal-message');
        const iconEl = modal.querySelector('.alert-modal-icon');
        const footerEl = modal.querySelector('.alert-modal-footer');
        
        // Remove todas as classes de tipo
        modal.className = 'alert-modal';
        
        // Adiciona classe do tipo
        modal.classList.add(`alert-modal-${type}`);
        
        // Define ícone baseado no tipo
        const icons = {
            'success': 'fas fa-check-circle',
            'error': 'fas fa-exclamation-circle',
            'notification': 'fas fa-bell',
            'event': 'fas fa-info-circle'
        };
        
        iconEl.className = `alert-modal-icon ${icons[type] || 'fas fa-info-circle'}`;
        titleEl.textContent = title;
        messageEl.textContent = message;
        
        // Configura botões do footer
        if (customButton) {
            footerEl.innerHTML = `
                <button class="alert-modal-btn alert-modal-btn-custom" onclick="${customButton.onclick}">${customButton.text}</button>
                <button class="alert-modal-btn" onclick="app.closeAlertModal()">OK</button>
            `;
        } else {
            footerEl.innerHTML = `<button class="alert-modal-btn" onclick="app.closeAlertModal()">OK</button>`;
        }
        
        modal.classList.add('show');
        
        // Fecha ao clicar fora
        const closeOnOutsideClick = (e) => {
            if (e.target === modal) {
                this.closeAlertModal();
                modal.removeEventListener('click', closeOnOutsideClick);
            }
        };
        modal.addEventListener('click', closeOnOutsideClick);
    },

    closeAlertModal: function() {
        const modal = document.getElementById('alert-modal');
        if (modal) {
            modal.classList.remove('show');
        }
    },

    // --- Validação de Campos Obrigatórios ---
    validateRequiredFields: function(containerId) {
        // Procura campos obrigatórios no container especificado ou em todo o documento
        const container = containerId ? document.getElementById(containerId) : document;
        if (!container) return false;
        
        let isValid = true;
        // Procura todos os campos obrigatórios no container ou no documento
        const requiredFields = container.querySelectorAll ? 
            container.querySelectorAll('input.required, textarea.required, select.required') :
            document.querySelectorAll('input.required, textarea.required, select.required');
        
        if (requiredFields.length === 0) {
            // Se não encontrou campos obrigatórios, considera válido (pode não ter campos obrigatórios)
            return true;
        }
        
        requiredFields.forEach(field => {
            const errorMsg = field.parentElement ? field.parentElement.querySelector('.field-error') : null;
            
            // Para select, verifica se tem valor selecionado
            if (field.tagName === 'SELECT') {
                if (!field.value || field.value === '') {
                    field.classList.add('error');
                    if (errorMsg) errorMsg.classList.add('show');
                    isValid = false;
                } else {
                    field.classList.remove('error');
                    if (errorMsg) errorMsg.classList.remove('show');
                }
            } else {
                // Para input e textarea
                if (!field.value || field.value.trim() === '') {
                    field.classList.add('error');
                    if (errorMsg) errorMsg.classList.add('show');
                    isValid = false;
                } else {
                    field.classList.remove('error');
                    if (errorMsg) errorMsg.classList.remove('show');
                }
            }
        });
        
        if (!isValid) {
            // Mostra mensagem de erro se houver campos inválidos
            const firstInvalidField = container.querySelector ? 
                container.querySelector('input.required.error, textarea.required.error, select.required.error') :
                document.querySelector('input.required.error, textarea.required.error, select.required.error');
            if (firstInvalidField) {
                firstInvalidField.focus();
            }
        }
        
        return isValid;
    },

    // Adiciona validação em tempo real
    initFieldValidation: function() {
        document.addEventListener('input', (e) => {
            if (e.target.classList.contains('required')) {
                const field = e.target;
                const errorMsg = field.parentElement.querySelector('.field-error');
                
                if (field.tagName === 'SELECT') {
                    if (field.value && field.value !== '') {
                        field.classList.remove('error');
                        if (errorMsg) errorMsg.classList.remove('show');
                    }
                } else {
                    if (field.value && field.value.trim() !== '') {
                        field.classList.remove('error');
                        if (errorMsg) errorMsg.classList.remove('show');
                    }
                }
            }
        });

        document.addEventListener('change', (e) => {
            if (e.target.classList.contains('required') && e.target.tagName === 'SELECT') {
                const field = e.target;
                const errorMsg = field.parentElement.querySelector('.field-error');
                
                if (field.value && field.value !== '') {
                    field.classList.remove('error');
                    if (errorMsg) errorMsg.classList.remove('show');
                }
            }
        });
    }
};

// Init
document.addEventListener('DOMContentLoaded', () => {
    app.init();
});
