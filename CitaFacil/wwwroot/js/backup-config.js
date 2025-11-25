(() => {
    const options = window.backupConfigOptions || {};
    const listUrl = options.listUrl || '/Admin/ListarDirectorios';
    const validateUrl = options.validateUrl || '/Admin/ValidarCarpeta';

    const directoryInput = document.getElementById('backup-directory-input');
    const selectButton = document.getElementById('select-directory-btn');
    const feedback = document.getElementById('directory-feedback');

    const modal = document.getElementById('directory-modal');
    const modalPath = document.getElementById('directory-modal-path');
    const modalList = document.getElementById('directory-list');
    const modalError = document.getElementById('directory-modal-error');
    const modalClose = document.getElementById('directory-modal-close');
    const modalCancel = document.getElementById('directory-modal-cancel');
    const modalUp = document.getElementById('directory-modal-up');
    const modalAccept = document.getElementById('directory-modal-accept');

    let currentPath = directoryInput?.value?.trim() ?? '';
    let selectedPath = null;

    function showModal() {
        if (!modal) {
            return;
        }

        modal.classList.remove('hidden');
        document.body.classList.add('overflow-hidden');
        const initialPath = (directoryInput?.value?.trim() ?? '') || currentPath || '';
        loadDirectory(initialPath);
    }

    function hideModal() {
        if (!modal) {
            return;
        }

        modal.classList.add('hidden');
        document.body.classList.remove('overflow-hidden');
        modalList.innerHTML = '';
        modalError.textContent = '';
        modalAccept.disabled = true;
        selectedPath = null;
    }

    async function loadDirectory(path) {
        modalError.textContent = '';
        modalList.innerHTML = '<li class="px-4 py-3 text-sm text-slate-500 dark:text-slate-400">Cargando...</li>';

        try {
            const url = new URL(listUrl, window.location.origin);
            if (path) {
                url.searchParams.set('path', path);
            }

            const response = await fetch(url, { method: 'GET' });
            if (!response.ok) {
                throw new Error('Respuesta no valida del servidor');
            }

            const data = await response.json().catch(() => null);
            if (!data) {
                throw new Error('No se pudo interpretar la respuesta.');
            }

            if (!data.success) {
                modalError.textContent = data.message || 'No fue posible leer la carpeta seleccionada.';
                modalList.innerHTML = '';
                return;
            }

            currentPath = data.currentPath || path;
            modalPath.textContent = currentPath;
            modalList.innerHTML = '';
            modalAccept.disabled = true;
            selectedPath = null;

            if (!data.directories || data.directories.length === 0) {
                modalList.innerHTML = '<li class="px-4 py-3 text-sm text-slate-500 dark:text-slate-400">No hay subcarpetas.</li>';
            } else {
                data.directories.forEach(dir => {
                    const li = document.createElement('li');
                    li.className = 'flex items-center justify-between px-4 py-3 text-sm hover:bg-slate-100 dark:hover:bg-slate-800 cursor-pointer';
                    li.dataset.path = dir.path;
                    li.innerHTML = `<span class="text-slate-700 dark:text-slate-200">${dir.name}</span><span class="material-symbols-outlined text-sm text-slate-400">chevron_right</span>`;
                    li.addEventListener('click', () => {
                        modalList.querySelectorAll('li').forEach(el => el.classList.remove('bg-primary/10', 'text-primary'));
                        li.classList.add('bg-primary/10', 'text-primary');
                        selectedPath = dir.path;
                        modalAccept.disabled = false;
                    });
                    li.addEventListener('dblclick', () => {
                        loadDirectory(dir.path);
                    });
                    modalList.appendChild(li);
                });
            }

            if (modalUp) {
                modalUp.disabled = !data.parentPath;
                modalUp.dataset.parent = data.parentPath || '';
            }
        } catch (error) {
            console.error('Error al obtener directorios', error);
            modalError.textContent = 'No se pudo cargar la carpeta. Verifica los permisos o intenta con otra ruta.';
            modalList.innerHTML = '';
        }
    }

    async function validateDirectory(path, { showMessages = true } = {}) {
        if (!path) {
            if (showMessages) {
                showFeedback('Selecciona primero una carpeta.', false);
            }
            return false;
        }

        try {
            const url = new URL(validateUrl, window.location.origin);
            url.searchParams.set('path', path);

            const response = await fetch(url, { method: 'GET' });
            if (!response.ok) {
                throw new Error('Respuesta no valida del servidor');
            }

            const data = await response.json().catch(() => null);
            if (!data || !data.success) {
                if (showMessages) {
                    showFeedback((data && data.message) || 'No se pudo validar la carpeta seleccionada.', false);
                }
                return false;
            }

            if (directoryInput) {
                directoryInput.value = data.path;
            }

            if (showMessages) {
                showFeedback('Carpeta validada correctamente.', true);
            }

            return true;
        } catch (error) {
            console.error('Error al validar la carpeta', error);
            if (showMessages) {
                showFeedback('Ocurrio un problema al validar la carpeta.', false);
            }
            return false;
        }
    }

    function showFeedback(message, success) {
        if (!feedback) {
            return;
        }

        feedback.textContent = message;
        feedback.classList.remove('hidden');
        feedback.classList.toggle('border-emerald-200', success);
        feedback.classList.toggle('bg-emerald-50', success);
        feedback.classList.toggle('text-emerald-700', success);
        feedback.classList.toggle('border-red-200', !success);
        feedback.classList.toggle('bg-red-50', !success);
        feedback.classList.toggle('text-red-700', !success);
    }

    if (selectButton) {
        selectButton.addEventListener('click', (evt) => {
            evt.preventDefault();
            showModal();
        });
    }

    if (modalClose) {
        modalClose.addEventListener('click', hideModal);
    }
    if (modalCancel) {
        modalCancel.addEventListener('click', hideModal);
    }
    if (modalUp) {
        modalUp.addEventListener('click', () => {
            const parent = modalUp.dataset.parent;
            if (parent) {
                loadDirectory(parent);
            }
        });
    }
    if (modalAccept) {
        modalAccept.addEventListener('click', async () => {
            if (!selectedPath) {
                return;
            }

            const valid = await validateDirectory(selectedPath);
            if (valid) {
                currentPath = selectedPath;
                hideModal();
            }
        });
    }

    if (modal) {
        modal.addEventListener('click', (evt) => {
            if (evt.target === modal) {
                hideModal();
            }
        });
    }

    if (directoryInput && directoryInput.value) {
        currentPath = directoryInput.value.trim();
        validateDirectory(currentPath, { showMessages: false });
    }
})();

