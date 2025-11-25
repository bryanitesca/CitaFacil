// CitaFacil - Core JavaScript functionality
class CitaFacil {
    constructor() {
        this.modals = new Map();
        this.notifications = [];
        this.init();
    }

    init() {
        this.setupEventListeners();
        this.initializeComponents();
        console.log('CitaFacil app initialized');
    }

    setupEventListeners() {
        // Global event listeners
        document.addEventListener('DOMContentLoaded', () => {
            this.initializeTooltips();
            this.initializeModals();
            this.initializeForms();
        });

        // Close modals with escape key
        document.addEventListener('keydown', (e) => {
            if (e.key === 'Escape') {
                this.closeAllModals();
            }
        });

        // Handle AJAX form submissions
        document.addEventListener('submit', (e) => {
            if (e.target.classList.contains('ajax-form')) {
                e.preventDefault();
                this.handleAjaxForm(e.target);
            }
        });
    }

    // Modal Management
    showModal(modalId, data = null) {
        const modal = document.getElementById(modalId);
        if (!modal) {
            console.error(`Modal ${modalId} not found`);
            return;
        }

        // Populate modal with data if provided
        if (data) {
            this.populateModal(modal, data);
        }

        modal.classList.remove('hidden');
        modal.classList.add('fade-in');
        document.body.style.overflow = 'hidden';
        
        this.modals.set(modalId, modal);
        
        // Focus first input
        const firstInput = modal.querySelector('input, select, textarea');
        if (firstInput) {
            setTimeout(() => firstInput.focus(), 100);
        }
    }

    closeModal(modalId) {
        const modal = this.modals.get(modalId) || document.getElementById(modalId);
        if (modal) {
            modal.classList.add('hidden');
            modal.classList.remove('fade-in');
            document.body.style.overflow = '';
            this.modals.delete(modalId);
            
            // Reset form if exists
            const form = modal.querySelector('form');
            if (form) {
                form.reset();
                this.clearFormErrors(form);
            }
        }
    }

    closeAllModals() {
        this.modals.forEach((modal, modalId) => {
            this.closeModal(modalId);
        });
    }

    populateModal(modal, data) {
        Object.keys(data).forEach(key => {
            const element = modal.querySelector(`[name="${key}"], [data-field="${key}"]`);
            if (element) {
                if (element.type === 'checkbox') {
                    element.checked = data[key];
                } else if (element.tagName === 'SELECT') {
                    element.value = data[key];
                } else if (element.hasAttribute('data-field')) {
                    element.textContent = data[key];
                } else {
                    element.value = data[key];
                }
            }
        });
    }

    // AJAX Form Handling
    async handleAjaxForm(form) {
        const submitBtn = form.querySelector('[type="submit"]');
        const originalText = submitBtn ? submitBtn.textContent : '';
        
        try {
            if (submitBtn) {
                submitBtn.disabled = true;
                submitBtn.textContent = 'Procesando...';
            }

            this.clearFormErrors(form);

            const formData = new FormData(form);
            const response = await fetch(form.action, {
                method: form.method || 'POST',
                body: formData,
                headers: {
                    'X-Requested-With': 'XMLHttpRequest'
                }
            });

            const result = await response.json();

            if (result.success) {
                this.showNotification(result.message || 'Operación exitosa', 'success');
                
                // Handle different success actions
                if (result.action === 'reload') {
                    window.location.reload();
                } else if (result.action === 'redirect' && result.url) {
                    window.location.href = result.url;
                } else if (result.action === 'close-modal' && result.modalId) {
                    this.closeModal(result.modalId);
                } else if (result.action === 'refresh-data') {
                    this.refreshData(result.target);
                }
            } else {
                if (result.errors) {
                    this.displayFormErrors(form, result.errors);
                } else {
                    this.showNotification(result.message || 'Error en la operación', 'error');
                }
            }
        } catch (error) {
            console.error('AJAX form error:', error);
            this.showNotification('Error de conexión. Intente nuevamente.', 'error');
        } finally {
            if (submitBtn) {
                submitBtn.disabled = false;
                submitBtn.textContent = originalText;
            }
        }
    }

    // Form Validation
    clearFormErrors(form) {
        const errorElements = form.querySelectorAll('.form-error');
        errorElements.forEach(el => el.textContent = '');
        
        const inputElements = form.querySelectorAll('.border-red-500');
        inputElements.forEach(el => {
            el.classList.remove('border-red-500');
            el.classList.add('border-gray-300');
        });
    }

    displayFormErrors(form, errors) {
        Object.keys(errors).forEach(fieldName => {
            const field = form.querySelector(`[name="${fieldName}"]`);
            const errorContainer = form.querySelector(`.error-${fieldName}`);
            
            if (field) {
                field.classList.remove('border-gray-300');
                field.classList.add('border-red-500');
            }
            
            if (errorContainer) {
                errorContainer.textContent = Array.isArray(errors[fieldName]) 
                    ? errors[fieldName][0] 
                    : errors[fieldName];
            }
        });
    }

    // Notification System
    showNotification(message, type = 'info', duration = 5000) {
        const notification = this.createNotificationElement(message, type);
        const container = this.getNotificationContainer();
        
        container.appendChild(notification);
        
        // Animate in
        setTimeout(() => {
            notification.classList.add('translate-x-0');
            notification.classList.remove('translate-x-full');
        }, 100);
        
        // Auto remove
        setTimeout(() => {
            this.removeNotification(notification);
        }, duration);
        
        this.notifications.push(notification);
    }

    createNotificationElement(message, type) {
        const notification = document.createElement('div');
        notification.className = `transform transition-transform duration-300 translate-x-full bg-white rounded-lg shadow-lg border-l-4 p-4 mb-2 ${this.getNotificationClasses(type)}`;
        
        notification.innerHTML = `
            <div class="flex items-center">
                <div class="flex-shrink-0">
                    ${this.getNotificationIcon(type)}
                </div>
                <div class="ml-3">
                    <p class="text-sm font-medium text-gray-900">${message}</p>
                </div>
                <div class="ml-auto pl-3">
                    <button type="button" class="inline-flex text-gray-400 hover:text-gray-600" onclick="citaFacil.removeNotification(this.closest('div').parentElement)">
                        <svg class="w-5 h-5" fill="currentColor" viewBox="0 0 20 20">
                            <path fill-rule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clip-rule="evenodd"></path>
                        </svg>
                    </button>
                </div>
            </div>
        `;
        
        return notification;
    }

    getNotificationClasses(type) {
        const classes = {
            success: 'border-green-400',
            error: 'border-red-400',
            warning: 'border-yellow-400',
            info: 'border-blue-400'
        };
        return classes[type] || classes.info;
    }

    getNotificationIcon(type) {
        const icons = {
            success: '<svg class="w-6 h-6 text-green-400" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"></path></svg>',
            error: '<svg class="w-6 h-6 text-red-400" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 14l2-2m0 0l2-2m-2 2l-2-2m2 2l2 2m7-2a9 9 0 11-18 0 9 9 0 0118 0z"></path></svg>',
            warning: '<svg class="w-6 h-6 text-yellow-400" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.964-.833-2.732 0L3.732 16.5c-.77.833.192 2.5 1.732 2.5z"></path></svg>',
            info: '<svg class="w-6 h-6 text-blue-400" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"></path></svg>'
        };
        return icons[type] || icons.info;
    }

    getNotificationContainer() {
        let container = document.getElementById('notification-container');
        if (!container) {
            container = document.createElement('div');
            container.id = 'notification-container';
            container.className = 'fixed top-4 right-4 z-50 max-w-sm';
            document.body.appendChild(container);
        }
        return container;
    }

    removeNotification(notification) {
        notification.classList.add('translate-x-full');
        setTimeout(() => {
            notification.remove();
            this.notifications = this.notifications.filter(n => n !== notification);
        }, 300);
    }

    // Utility Functions
    initializeComponents() {
        // Initialize tooltips
        this.initializeTooltips();
        
        // Initialize dropdowns
        this.initializeDropdowns();
        
        // Initialize date pickers
        this.initializeDatePickers();
    }

    initializeModals() {
        // Setup modal close buttons
        document.querySelectorAll('[data-modal-close]').forEach(button => {
            button.addEventListener('click', (e) => {
                const modalId = e.target.getAttribute('data-modal-close');
                this.closeModal(modalId);
            });
        });

        // Setup modal open buttons
        document.querySelectorAll('[data-modal-open]').forEach(button => {
            button.addEventListener('click', (e) => {
                e.preventDefault();
                const modalId = e.target.getAttribute('data-modal-open');
                this.showModal(modalId);
            });
        });

        // Close modal when clicking overlay
        document.querySelectorAll('.modal-overlay').forEach(overlay => {
            overlay.addEventListener('click', (e) => {
                if (e.target === overlay) {
                    const modal = overlay.closest('[id]');
                    if (modal) {
                        this.closeModal(modal.id);
                    }
                }
            });
        });
    }

    initializeForms() {
        // Real-time validation
        document.querySelectorAll('input[required], select[required]').forEach(input => {
            input.addEventListener('blur', () => {
                this.validateField(input);
            });
        });

        // Phone number formatting
        document.querySelectorAll('input[type="tel"]').forEach(input => {
            input.addEventListener('input', (e) => {
                this.formatPhoneNumber(e.target);
            });
        });
    }

    initializeTooltips() {
        // Simple tooltip implementation
        document.querySelectorAll('[data-tooltip]').forEach(element => {
            element.addEventListener('mouseenter', this.showTooltip);
            element.addEventListener('mouseleave', this.hideTooltip);
        });
    }

    initializeDropdowns() {
        document.querySelectorAll('.dropdown').forEach(dropdown => {
            const trigger = dropdown.querySelector('.dropdown-trigger');
            const menu = dropdown.querySelector('.dropdown-menu');
            
            if (trigger && menu) {
                trigger.addEventListener('click', () => {
                    menu.classList.toggle('hidden');
                });
                
                // Close when clicking outside
                document.addEventListener('click', (e) => {
                    if (!dropdown.contains(e.target)) {
                        menu.classList.add('hidden');
                    }
                });
            }
        });
    }

    initializeDatePickers() {
        // Basic date picker setup
        document.querySelectorAll('input[type="date"]').forEach(input => {
            // Set min date to today
            if (!input.min) {
                input.min = new Date().toISOString().split('T')[0];
            }
        });
    }

    validateField(field) {
        const value = field.value.trim();
        let isValid = true;
        let message = '';

        // Required validation
        if (field.hasAttribute('required') && !value) {
            isValid = false;
            message = 'Este campo es requerido';
        }

        // Email validation
        if (field.type === 'email' && value) {
            const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
            if (!emailRegex.test(value)) {
                isValid = false;
                message = 'Formato de email inválido';
            }
        }

        // Phone validation
        if (field.type === 'tel' && value) {
            const phoneRegex = /^\d{10}$/;
            if (!phoneRegex.test(value.replace(/\D/g, ''))) {
                isValid = false;
                message = 'Número de teléfono inválido (10 dígitos)';
            }
        }

        // Update field appearance
        if (isValid) {
            field.classList.remove('border-red-500');
            field.classList.add('border-gray-300');
        } else {
            field.classList.remove('border-gray-300');
            field.classList.add('border-red-500');
        }

        // Show/hide error message
        const errorElement = field.parentElement.querySelector('.form-error');
        if (errorElement) {
            errorElement.textContent = message;
        }

        return isValid;
    }

    formatPhoneNumber(input) {
        let value = input.value.replace(/\D/g, '');
        if (value.length >= 10) {
            value = value.substring(0, 10);
        }
        input.value = value;
    }

    showTooltip(e) {
        const element = e.target;
        const text = element.getAttribute('data-tooltip');
        
        const tooltip = document.createElement('div');
        tooltip.className = 'absolute z-50 px-2 py-1 text-sm text-white bg-gray-900 rounded shadow-lg';
        tooltip.textContent = text;
        tooltip.id = 'tooltip';
        
        document.body.appendChild(tooltip);
        
        const rect = element.getBoundingClientRect();
        tooltip.style.left = `${rect.left + (rect.width / 2) - (tooltip.offsetWidth / 2)}px`;
        tooltip.style.top = `${rect.top - tooltip.offsetHeight - 5}px`;
    }

    hideTooltip() {
        const tooltip = document.getElementById('tooltip');
        if (tooltip) {
            tooltip.remove();
        }
    }

    async refreshData(target) {
        // Implement data refresh logic based on target
        console.log('Refreshing data for:', target);
    }

    // Avatar utilities
    generateInitials(name) {
        if (!name) return 'UN';
        
        const parts = name.trim().split(' ');
        if (parts.length >= 2) {
            return (parts[0][0] + parts[1][0]).toUpperCase();
        }
        return parts[0].substring(0, 2).toUpperCase();
    }

    createAvatar(name, photoUrl = null, size = 'md') {
        const avatar = document.createElement('div');
        avatar.className = `avatar avatar-${size}`;
        
        if (photoUrl) {
            avatar.innerHTML = `<img src="${photoUrl}" alt="${name}" class="w-full h-full object-cover rounded-full">`;
        } else {
            avatar.textContent = this.generateInitials(name);
        }
        
        return avatar;
    }
}

// Initialize the app
const citaFacil = new CitaFacil();

// Export for use in other scripts
window.citaFacil = citaFacil;