import { clearForm } from './reg-form-ui.js';

const form = document.getElementById('regForm');
if (!form) throw new Error("regForm not found");

const sendingModalEl = document.getElementById('sendingModal');
const successModalEl = document.getElementById('successModal');
const errorModalEl = document.getElementById('submitErrorModal');
const errorTextEl = document.getElementById('submitErrorText');
const summaryEl = document.getElementById('validationSummary');

const sendingModal = sendingModalEl ? new bootstrap.Modal(sendingModalEl, { backdrop: "static", keyboard: true }) : null;
const successModal = successModalEl ? new bootstrap.Modal(successModalEl, { backdrop: "static", keyboard: true }) : null;
const errorModal = errorModalEl ? new bootstrap.Modal(errorModalEl, { backdrop: "static", keyboard: true }) : null;

function setSummary(messages) {
    if (!summaryEl) return;
    if (!messages || messages.length === 0) {
        summaryEl.innerHTML = '';
        return;
    }
    summaryEl.innerHTML = `<ul class="mb-0">${messages.map(m => `<li>${escapeHtml(m)}</li>`).join('')}</ul>`;
}

function clearServerErrors() {
    setSummary([]);

    // Clear field-level spans
    form.querySelectorAll('span[data-valmsg-for]').forEach(span => {
        span.textContent = '';
        span.classList.remove('field-validation-error');
        span.classList.add('field-validation-valid');
    });

    // Clear input-styling
    form.querySelectorAll('.is-invalid').forEach(el => el.classList.remove('is-invalid'));
}

function applyModelStateErrors(validation) {
    // validation: { "FirstName": ["..."], "Medicines[0].MedicineName": ["..."], ... }
    const summary = [];

    for (const key in validation) {
        const messages = validation[key] || [];
        messages.forEach(m => summary.push(m));

        // Set message i span[data-valmsg-for="<key>"]
        const span = form.querySelector(`span[data-valmsg-for="${cssEscape(key)}"]`);
        if (span && messages.length) {
            span.textContent = messages[0];
            span.classList.remove('field-validation-valid');
            span.classList.add('field-validation-error');
        }

        // Mark input with name="<key>"
        const input = form.querySelector(`[name="${cssEscape(key)}"]`);
        if (input) input.classList.add('is-invalid');
    }

    setSummary(summary);
}

function showError(message) {
    if (errorTextEl) errorTextEl.textContent = message || 'Ett okänt fel inträffade.';
    errorModal?.show();
}

function escapeHtml(str) {
    return String(str)
        .replaceAll('&', '&amp;')
        .replaceAll('<', '&lt;')
        .replaceAll('>', '&gt;')
        .replaceAll('"', '&quot;')
        .replaceAll("'", '&#039;');
}

// Minimal cssEscape fallback (for keys with [] etc)
function cssEscape(s) {
    if (window.CSS && CSS.escape) return CSS.escape(s);
    return String(s).replace(/(["\\#.:,[\]=>+~*^$(){}|/])/g, '\\$1');
}

function setBusy(isBusy) {
    if (isBusy) {
        sendingModal.show();
    }
    else {
        requestAnimationFrame(() => sendingModal.hide());
    }
}

async function readJsonSafe(resp) {
    const ct = resp.headers.get('content-type') || '';
    if (ct.includes('application/json')) return await resp.json();
    return null;
}

form.addEventListener('submit', async function (e) {
    e.preventDefault();

    clearServerErrors();
    setBusy(true);

    try {
        const fd = new FormData(form);

        const resp = await fetch(form.action, {
            method: form.method,
            body: fd,
            headers: {
                'Accept': 'application/json'
            }
        });

        const data = await readJsonSafe(resp);
        if (resp.ok) {
            sendingModal.hide();
            successModal?.show();

            // Auto-close + clear form
            setTimeout(() => successModal?.hide(), 2500);
            clearForm();

            return;
        }

        // 400 = validation error
        if (resp.status === 400 && data && data.validation) {
            sendingModal.hide();
            applyModelStateErrors(data.validation);
            return;
        }

        // Other error (500 etc)
        showError((data && data.message) ? data.message : `Serverfel (${resp.status}). Försök igen.`);
    }
    catch (err) {
        showError('kunde inte kontakta servern. Kontrollera anslutningen och försök igen.');
    }
    finally {
        setBusy(false);
    }
});
