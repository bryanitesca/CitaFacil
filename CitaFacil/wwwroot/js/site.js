// Utilidades globales para la aplicación
(function () {
    const BRAND_COLOR = '#135bec';
    const CANCEL_COLOR = '#6b7280';

    function normalizeOptions(input, defaults) {
        if (typeof input === 'string') {
            return { ...defaults, text: input };
        }
        return { ...defaults, ...(input || {}) };
    }

    function ensureSwal() {
        return typeof window !== 'undefined' && typeof window.Swal !== 'undefined';
    }

    window.showAppAlert = function (options = {}, defaultIcon = 'info') {
        const normalized = normalizeOptions(options, {
            title: 'Aviso',
            text: '',
            icon: defaultIcon,
            confirmButtonText: 'Aceptar'
        });

        normalized.icon = normalized.icon || defaultIcon;
        normalized.confirmButtonText = normalized.confirmButtonText || 'Aceptar';
        normalized.confirmButtonColor = normalized.confirmButtonColor || BRAND_COLOR;

        if (ensureSwal()) {
            return Swal.fire(normalized);
        }

        alert(normalized.text || normalized.title || '');
        return Promise.resolve();
    };

    window.showAppConfirm = function (options = {}) {
        const normalized = normalizeOptions(options, {
            title: '¿Estás seguro?',
            text: '',
            icon: 'question',
            confirmButtonText: 'Aceptar',
            cancelButtonText: 'Cancelar'
        });

        normalized.showCancelButton = true;
        normalized.confirmButtonColor = normalized.confirmButtonColor || BRAND_COLOR;
        normalized.cancelButtonColor = normalized.cancelButtonColor || CANCEL_COLOR;

        if (ensureSwal()) {
            return Swal.fire(normalized).then(result => result.isConfirmed);
        }

        const message = normalized.text || normalized.title || '';
        return Promise.resolve(window.confirm(message));
    };

    window.showAppPrompt = function (options = {}) {
        const normalized = normalizeOptions(options, {
            title: 'Ingrese un valor',
            text: '',
            icon: 'question',
            confirmButtonText: 'Guardar',
            cancelButtonText: 'Cancelar',
            input: 'text',
            inputPlaceholder: '',
            inputValue: ''
        });

        normalized.showCancelButton = true;
        normalized.confirmButtonColor = normalized.confirmButtonColor || BRAND_COLOR;
        normalized.cancelButtonColor = normalized.cancelButtonColor || CANCEL_COLOR;

        if (ensureSwal()) {
            return Swal.fire(normalized).then(result => (result.isConfirmed ? result.value : null));
        }

        const fallback = window.prompt(normalized.text || normalized.title || '', normalized.inputValue || '');
        return Promise.resolve(fallback === null ? null : fallback);
    };

    window.showAppToast = function (options = {}) {
        if (!ensureSwal()) {
            console.warn('SweetAlert2 no está disponible para mostrar el toast.');
            return Promise.resolve();
        }

        const normalized = {
            toast: true,
            position: options.position || 'top-end',
            timer: options.timer || 3000,
            timerProgressBar: true,
            showConfirmButton: false,
            icon: options.icon || 'info',
            title: options.title || ''
        };

        return Swal.fire(normalized);
    };
})();
