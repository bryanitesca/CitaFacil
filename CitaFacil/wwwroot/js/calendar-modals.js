// Calendar Modals JavaScript
// Handles modal functionality for appointment creation and details

// Global variables
let currentAppointmentId = null;
let selectedDate = null;

function getRequestVerificationToken() {
    const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
    return tokenInput ? tokenInput.value : '';
}

// Show create appointment modal
function showCreateAppointmentModal(dateStr = null) {
    selectedDate = dateStr;
    const modal = document.getElementById('modal-crear-cita');
    const formCrear = document.getElementById('form-crear-cita');

    if (!modal || !formCrear) {
        return;
    }
    
    // Reset form
    formCrear.reset();
    
    // Set date if provided
    if (dateStr) {
        const dateInput = document.querySelector('input[name="fecha"]');
        if (dateInput) {
            dateInput.value = dateStr;
        }
    }
    
    // Load patients for dropdown
    loadPatients();
    
    // Show modal
    modal.classList.remove('hidden');
    document.body.style.overflow = 'hidden';
}

// Hide create appointment modal
function hideCreateAppointmentModal() {
    const modal = document.getElementById('modal-crear-cita');
    if (!modal) {
        return;
    }

    modal.classList.add('hidden');
    document.body.style.overflow = 'auto';
}

// Show appointment details modal
function showAppointmentDetails(appointmentId) {
    currentAppointmentId = appointmentId;
    
    // Fetch appointment details via AJAX
    fetch(`/Citas/GetDetailsAjax/${appointmentId}`)
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                populateDetailsModal(data.cita);
                const modal = document.getElementById('modal-detalles-cita');
                modal.classList.remove('hidden');
                document.body.style.overflow = 'hidden';
            } else {
                showNotification('Error al cargar los detalles de la cita', 'error');
            }
        })
        .catch(error => {
            console.error('Error:', error);
            showNotification('Error al cargar los detalles de la cita', 'error');
        });
}

// Hide appointment details modal
function hideAppointmentDetailsModal() {
    const modal = document.getElementById('modal-detalles-cita');
    modal.classList.add('hidden');
    document.body.style.overflow = 'auto';
    currentAppointmentId = null;
    resetCancelPanel();
}

// Load patients for dropdown
function loadPatients() {
    fetch('/Citas/GetPacientesAjax')
        .then(response => response.json())
        .then(data => {
            const select = document.getElementById('paciente-select');
            select.innerHTML = '<option value="">Selecciona un paciente</option>';
            
            data.forEach(paciente => {
                const option = document.createElement('option');
                option.value = paciente.id;
                option.textContent = `${paciente.nombre} ${paciente.apellido}`;
                select.appendChild(option);
            });
        })
        .catch(error => {
            console.error('Error loading patients:', error);
        });
}

// Populate details modal with appointment data
function populateDetailsModal(cita) {
    // Patient details
    document.getElementById('detalle-paciente-nombre').textContent = 
        `${cita.paciente.nombre} ${cita.paciente.apellido}`;
    
    document.getElementById('detalle-paciente-nacimiento').textContent = 
        new Date(cita.paciente.fecha_nacimiento).toLocaleDateString('es-ES');
    
    document.getElementById('detalle-paciente-genero').textContent = 
        cita.paciente.genero || 'No especificado';
    
    // Appointment details
    document.getElementById('detalle-cita-fecha').textContent = 
        new Date(cita.fecha).toLocaleDateString('es-ES', {
            weekday: 'long',
            year: 'numeric',
            month: 'long',
            day: 'numeric'
        });
    
    document.getElementById('detalle-cita-hora').textContent = cita.hora;
    
    document.getElementById('detalle-cita-duracion').textContent = 
        `${cita.duracion_minutos} minutos`;
    
    document.getElementById('detalle-cita-motivo').textContent = 
        cita.motivo || 'No especificado';
    
    if (cita.notas) {
        document.getElementById('detalle-cita-notas').textContent = cita.notas;
        document.getElementById('detalle-notas-container').style.display = 'flex';
    } else {
        document.getElementById('detalle-notas-container').style.display = 'none';
    }

    const cancelInfo = document.getElementById('detalle-cita-cancelacion');
    const cancelInfoText = document.getElementById('detalle-cita-motivo-cancelacion');
    if (cancelInfo && cancelInfoText) {
        if (cita.motivo_cancelacion) {
            cancelInfo.style.display = 'flex';
            cancelInfoText.textContent = cita.motivo_cancelacion;
        } else {
            cancelInfo.style.display = 'none';
            cancelInfoText.textContent = '';
        }
    }
    
    // Status badge
    const statusBadge = document.getElementById('detalle-cita-estado');
    statusBadge.textContent = getStatusText(cita.estado);
    statusBadge.className = `inline-flex items-center px-2 py-1 rounded-full text-xs font-medium ${getStatusClass(cita.estado)}`;
    
    // Update action buttons based on status
    resetCancelPanel();
    updateActionButtons(cita.estado);
}

// Get status text in Spanish
function getStatusText(status) {
    const statusTexts = {
        'PENDIENTE': 'Pendiente',
        'CONFIRMADA': 'Confirmada',
        'INICIADA': 'En consulta',
        'COMPLETADA': 'Completada',
        'CANCELADA': 'Cancelada'
    };
    return statusTexts[status] || status;
}

// Get status CSS classes
function getStatusClass(status) {
    const statusClasses = {
        'PENDIENTE': 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900/20 dark:text-yellow-400',
        'CONFIRMADA': 'bg-green-100 text-green-800 dark:bg-green-900/20 dark:text-green-400',
        'INICIADA': 'bg-sky-100 text-sky-800 dark:bg-sky-900/20 dark:text-sky-400',
        'COMPLETADA': 'bg-blue-100 text-blue-800 dark:bg-blue-900/20 dark:text-blue-400',
        'CANCELADA': 'bg-red-100 text-red-800 dark:bg-red-900/20 dark:text-red-400'
    };
    return statusClasses[status] || 'bg-gray-100 text-gray-800 dark:bg-gray-900/20 dark:text-gray-400';
}

function showCancelPanel() {
    const panel = document.getElementById('cancel-reason-panel');
    if (panel) {
        panel.classList.remove('hidden');
    }

    const textarea = document.getElementById('cancel-reason-text');
    if (textarea) {
        textarea.focus();
    }

    const error = document.getElementById('cancel-reason-error');
    if (error) {
        error.classList.add('hidden');
        error.textContent = '';
    }
}

function resetCancelPanel() {
    const panel = document.getElementById('cancel-reason-panel');
    if (panel) {
        panel.classList.add('hidden');
    }

    const textarea = document.getElementById('cancel-reason-text');
    if (textarea) {
        textarea.value = '';
    }

    const error = document.getElementById('cancel-reason-error');
    if (error) {
        error.classList.add('hidden');
        error.textContent = '';
    }
}

// Update action buttons based on appointment status
function updateActionButtons(status) {
    const confirmBtn = document.getElementById('btn-confirmar-cita');
    const cancelBtn = document.getElementById('btn-cancelar-cita');

    if (confirmBtn) {
        confirmBtn.style.display = status === 'PENDIENTE' ? 'flex' : 'none';
    }

    if (cancelBtn) {
        const hideCancel = status === 'CANCELADA' || status === 'COMPLETADA';
        cancelBtn.style.display = hideCancel ? 'none' : 'flex';
    }
}

// Create appointment
function createAppointment(formData) {
    fetch('/Citas/CreateAjax', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': getRequestVerificationToken()
        },
        body: JSON.stringify(formData)
    })
    .then(response => response.json())
    .then(data => {
        if (data.success) {
            hideCreateAppointmentModal();
            showNotification('Cita creada exitosamente', 'success');
            if (window.doctorCalendar) {
                window.doctorCalendar.refetchEvents();
            }
        } else {
            showNotification(data.message || 'Error al crear la cita', 'error');
        }
    })
    .catch(error => {
        console.error('Error:', error);
        showNotification('Error al crear la cita', 'error');
    });
}

// Change appointment status
function changeAppointmentStatus(appointmentId, newStatus, motivo = null) {
    fetch(`/Citas/UpdateStatusAjax/${appointmentId}`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': getRequestVerificationToken()
        },
        body: JSON.stringify({
            estado: newStatus,
            motivo_cancelacion: motivo
        })
    })
    .then(response => response.json())
    .then(data => {
        if (data.success) {
            resetCancelPanel();
            hideAppointmentDetailsModal();
            showNotification('Estado de la cita actualizado', 'success');
            if (window.doctorCalendar) {
                window.doctorCalendar.refetchEvents();
            }
        } else {
            showNotification(data.message || 'Error al actualizar la cita', 'error');
        }
    })
    .catch(error => {
        console.error('Error:', error);
        showNotification('Error al actualizar la cita', 'error');
    });
}

// Show notification
function showNotification(message, type = 'info') {
    // Create a simple notification (you can enhance this)
    const notification = document.createElement('div');
    notification.className = `fixed top-4 right-4 z-50 p-4 rounded-lg shadow-lg max-w-sm ${
        type === 'success' ? 'bg-green-500 text-white' :
        type === 'error' ? 'bg-red-500 text-white' :
        'bg-blue-500 text-white'
    }`;
    notification.textContent = message;
    
    document.body.appendChild(notification);
    
    setTimeout(() => {
        notification.remove();
    }, 3000);
}

// Initialize event listeners when DOM is ready
document.addEventListener('DOMContentLoaded', function() {
    // Create appointment modal event listeners
    const createModal = document.getElementById('modal-crear-cita');
    if (createModal) {
        // Close buttons
        document.getElementById('btn-cerrar-crear')?.addEventListener('click', hideCreateAppointmentModal);
        document.getElementById('btn-cancelar-crear')?.addEventListener('click', hideCreateAppointmentModal);
        
        // Form submission
        document.getElementById('form-crear-cita')?.addEventListener('submit', function(e) {
            e.preventDefault();
            
            const formData = new FormData(this);
            const data = Object.fromEntries(formData);
            
            createAppointment(data);
        });
        
        // Close on background click
        createModal.addEventListener('click', function(e) {
            if (e.target === this) {
                hideCreateAppointmentModal();
            }
        });
    }
    
    // Details modal event listeners
    const detailsModal = document.getElementById('modal-detalles-cita');
    if (detailsModal) {
        // Close buttons
        document.getElementById('btn-cerrar-detalles-cita')?.addEventListener('click', hideAppointmentDetailsModal);
        document.getElementById('btn-cerrar-detalles-footer')?.addEventListener('click', hideAppointmentDetailsModal);
        
            const confirmarBtn = document.getElementById('btn-confirmar-cita');
            if (confirmarBtn) {
                confirmarBtn.addEventListener('click', function() {
                    if (currentAppointmentId) {
                        changeAppointmentStatus(currentAppointmentId, 'CONFIRMADA');
                    }
                });
            }

            const cancelarBtn = document.getElementById('btn-cancelar-cita');
            if (cancelarBtn) {
                cancelarBtn.addEventListener('click', function() {
                    showCancelPanel();
                });
            }

            const cancelarEnviarBtn = document.getElementById('btn-enviar-cancelacion');
            if (cancelarEnviarBtn) {
                cancelarEnviarBtn.addEventListener('click', function() {
                    if (!currentAppointmentId) {
                        return;
                    }

                    const reasonInput = document.getElementById('cancel-reason-text');
                    const reasonError = document.getElementById('cancel-reason-error');
                    const reason = reasonInput ? reasonInput.value.trim() : '';

                    if (!reason) {
                        if (reasonError) {
                            reasonError.textContent = 'Ingresa un motivo para cancelar la cita.';
                            reasonError.classList.remove('hidden');
                        }
                        return;
                    }

                    if (reasonError) {
                        reasonError.classList.add('hidden');
                        reasonError.textContent = '';
                    }

                    changeAppointmentStatus(currentAppointmentId, 'CANCELADA', reason);
                });
            }

            const cancelarCerrarBtn = document.getElementById('btn-cerrar-cancelacion');
            if (cancelarCerrarBtn) {
                cancelarCerrarBtn.addEventListener('click', resetCancelPanel);
            }
        
        // Close on background click
        detailsModal.addEventListener('click', function(e) {
            if (e.target === this) {
                hideAppointmentDetailsModal();
            }
        });
    }
    
    // Confirm delete modal event listeners
    // ESC key to close modals
    document.addEventListener('keydown', function(e) {
        if (e.key === 'Escape') {
            hideCreateAppointmentModal();
            hideAppointmentDetailsModal();
        }
    });
});