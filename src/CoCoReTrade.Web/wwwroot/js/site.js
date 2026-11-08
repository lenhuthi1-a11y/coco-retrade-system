(() => {
  const forms = Array.from(document.querySelectorAll('form[data-favorite]'));
  let feedbackTimer;
  function showError(message) {
    let feedback = document.getElementById('favoriteFeedback');
    if (!feedback) {
      feedback = document.createElement('div');
      feedback.id = 'favoriteFeedback';
      feedback.className = 'cart-feedback is-error';
      feedback.setAttribute('role', 'alert');
      document.body.append(feedback);
    }
    feedback.textContent = message;
    feedback.hidden = false;
    clearTimeout(feedbackTimer);
    feedbackTimer = setTimeout(() => { feedback.hidden = true; }, 4000);
  }
  forms.forEach(form => form.addEventListener('submit', async event => {
    event.preventDefault();
    const button = form.querySelector('button[type="submit"]');
    if (button.disabled) return;
    const productId = form.elements.namedItem('productId').value;
    const related = forms.filter(item => item.elements.namedItem('productId').value === productId);
    related.forEach(item => { item.querySelector('button[type="submit"]').disabled = true; });
    try {
      const response = await fetch(form.action, {
        method: 'POST', body: new FormData(form), credentials: 'same-origin',
        headers: { Accept: 'application/json' }
      });
      if (!response.headers.get('content-type')?.includes('application/json')) {
        showError('Phiên làm việc đã thay đổi. Vui lòng tải lại trang rồi thử lại.');
        return;
      }
      const result = await response.json();
      if (!response.ok || typeof result.selected !== 'boolean') {
        showError(result.error || 'Không cập nhật được yêu thích. Vui lòng thử lại.');
        return;
      }
      related.forEach(item => {
        const toggle = item.querySelector('button[type="submit"]');
        item.elements.namedItem('selected').value = String(!result.selected);
        toggle.classList.toggle('is-saved', result.selected);
        toggle.setAttribute('aria-pressed', String(result.selected));
        toggle.setAttribute('aria-label', `${result.selected ? 'Bỏ lưu' : 'Lưu'} ${item.dataset.productName}`);
        const label = item.querySelector('[data-favorite-label]');
        if (label) label.textContent = result.selected ? 'Đã lưu · Bỏ lưu món đồ' : 'Lưu món đồ để xem sau';
      });
    } catch {
      showError('Không kết nối được. Vui lòng thử lại.');
    } finally {
      related.forEach(item => { item.querySelector('button[type="submit"]').disabled = false; });
    }
  }));
})();

document.querySelectorAll('input[data-preview]').forEach(input => {
  let selected = [];
  const target = document.getElementById(input.dataset.preview);
  if (!target) return;
  const maxFiles = Number(input.dataset.maxFiles) || 10;
  const dropzone = input.closest('[data-photo-dropzone]');
  const addButton = input.closest('section')?.querySelector('[data-photo-add]');
  addButton?.addEventListener('click', () => input.click());
  const existing = input.closest('form')?.querySelector('#existingPhotos');
  const existingCount = () => existing?.querySelectorAll('[data-existing-photo]:not([hidden])').length || 0;
  function render() {
    const transfer = new DataTransfer();
    selected.forEach(file => transfer.items.add(file));
    input.files = transfer.files;
    target.replaceChildren();
    selected.forEach((file, index) => {
      const item = document.createElement('div');
      item.className = 'photo-preview-item';
      const image = document.createElement('img');
      image.alt = file.name;
      image.src = URL.createObjectURL(file);
      image.onload = () => URL.revokeObjectURL(image.src);
      const remove = document.createElement('button');
      remove.type = 'button';
      remove.className = 'photo-preview-remove';
      remove.setAttribute('aria-label', `Xóa ảnh ${file.name}`);
      remove.textContent = '×';
      remove.addEventListener('click', () => { selected.splice(index, 1); render(); });
      item.append(image, remove);
      target.append(item);
    });
    if (dropzone) dropzone.hidden = selected.length > 0;
    if (addButton) addButton.disabled = selected.length + existingCount() >= maxFiles;
  }
  input.addEventListener('change', () => {
    const incoming = Array.from(input.files);
    for (const file of incoming) {
      if (!['image/jpeg', 'image/png', 'image/webp'].includes(file.type)) continue;
      if (selected.length + existingCount() >= maxFiles) {
        window.alert(`Chỉ được chọn tối đa ${maxFiles} ảnh.`);
        break;
      }
      if (!selected.some(current => current.name === file.name && current.size === file.size && current.lastModified === file.lastModified))
        selected.push(file);
    }
    render();
  });
  if (dropzone) {
    dropzone.addEventListener('dragover', event => {
      event.preventDefault();
      dropzone.classList.add('is-dragover');
    });
    dropzone.addEventListener('dragleave', () => dropzone.classList.remove('is-dragover'));
    dropzone.addEventListener('drop', event => {
      event.preventDefault();
      dropzone.classList.remove('is-dragover');
      if (!event.dataTransfer?.files.length) return;
      input.files = event.dataTransfer.files;
      input.dispatchEvent(new Event('change', { bubbles: true }));
    });
  }
  existing?.querySelectorAll('[data-remove-existing]').forEach(button => {
    button.addEventListener('click', () => {
      const item = button.closest('[data-existing-photo]');
      item.hidden = true;
      item.querySelector('input[name="RetainedImageIds"]').disabled = true;
      render();
    });
  });
});

document.querySelectorAll('[data-held-countdown]').forEach(element => {
  const deadline = Number(element.dataset.heldCountdown);
  const refresh = () => {
    const minutes = Math.max(0, Math.ceil((deadline - Date.now()) / 60000));
    element.textContent = minutes > 0 ? `${Math.floor(minutes / 60)} giờ ${minutes % 60} phút` : 'Đã hết hạn';
  };
  refresh();
  const timer = window.setInterval(() => {
    refresh();
    if (Date.now() >= deadline) window.clearInterval(timer);
  }, 30000);
});

document.querySelectorAll('form.listing-form').forEach(form => {
  const acknowledgement = form.querySelector('input[data-listing-ack]');
  const submit = form.querySelector('button[data-listing-submit]');
  if (!acknowledgement || !submit) return;
  const sync = () => { submit.disabled = !acknowledgement.checked; };
  acknowledgement.addEventListener('change', sync);
  sync();
});

document.querySelectorAll('button[data-inspection-publish]').forEach(button => {
  const approved = button.form?.querySelector('input[type="checkbox"][name="Passed"]');
  if (!approved) return;
  const sync = () => { button.disabled = !approved.checked; };
  approved.addEventListener('change', sync);
  sync();
});

document.querySelectorAll('input[data-vnd-price]').forEach(input => {
  const format = value => value.replace(/[^0-9]/g, '').replace(/^0+(?=[0-9])/, '')
    .replace(/\B(?=([0-9]{3})+(?![0-9]))/g, ',');
  input.value = input.value === '0' ? '' : format(input.value);
  let composing = false;
  const preview = document.createElement('span');
  preview.className = 'vnd-price-preview';
  preview.setAttribute('aria-hidden', 'true');
  input.after(preview);
  const showPreview = () => {
    preview.textContent = format(input.value);
    input.classList.add('vnd-price-composing');
  };
  const hidePreview = () => {
    input.classList.remove('vnd-price-composing');
    preview.textContent = '';
  };
  const formatInput = () => {
    const value = input.value;
    const caret = input.selectionStart ?? value.length;
    const digitsBeforeCaret = value.slice(0, caret).replace(/[^0-9]/g, '')
      .replace(/^0+(?=[0-9])/, '').length;
    const formatted = format(value);
    // Avoid resetting the IME's editing buffer when no formatting is needed.
    if (formatted === value) return;
    input.value = formatted;
    let position = 0;
    let digits = 0;
    while (position < formatted.length && digits < digitsBeforeCaret) {
      if (/[0-9]/.test(formatted[position])) digits++;
      position++;
    }
    input.setSelectionRange(position, position);
  };
  // Wait until the IME commits its text before changing the value or caret.
  input.addEventListener('compositionstart', () => {
    composing = true;
    showPreview();
  });
  input.addEventListener('compositionend', () => {
    composing = false;
    formatInput();
    hidePreview();
  });
  input.addEventListener('input', event => {
    if (composing || event.isComposing) showPreview();
    else {
      formatInput();
      hidePreview();
    }
  });
  input.addEventListener('blur', () => {
    if (!composing) formatInput();
    hidePreview();
  });
});

new Set(Array.from(document.querySelectorAll('input[data-vnd-price]'), input => input.form).filter(Boolean)).forEach(form => {
  form.addEventListener('formdata', event => {
    form.querySelectorAll('input[data-vnd-price]').forEach(input => {
      if (input.name) event.formData.set(input.name, input.value.replace(/,/g, ''));
    });
  });
});

(() => {
  const badges = document.querySelectorAll('[data-cart-count]');
  let cartUpdated = false;
  function showCount(count) {
    for (const badge of badges) {
      badge.hidden = count < 1;
      badge.textContent = count > 99 ? '99+' : String(count);
      badge.closest('.cart-link')?.setAttribute('aria-label', `Giỏ hàng: ${count} món`);
    }
  }
  if (badges.length) {
    fetch('/Cart/Count', { credentials: 'same-origin', cache: 'no-store', headers: { Accept: 'application/json' } })
      .then(response => response.ok ? response.json() : null)
      .then(data => { if (data && !cartUpdated) showCount(data.count); })
      .catch(() => {});
  }
  let feedbackTimer;
  function showFeedback(message, error = false) {
    let feedback = document.getElementById('cartFeedback');
    if (!feedback) {
      feedback = document.createElement('div');
      feedback.id = 'cartFeedback';
      feedback.className = 'cart-feedback';
      feedback.setAttribute('role', 'status');
      feedback.setAttribute('aria-live', 'polite');
      document.body.append(feedback);
    }
    feedback.textContent = message;
    feedback.classList.toggle('is-error', error);
    feedback.hidden = false;
    clearTimeout(feedbackTimer);
    feedbackTimer = setTimeout(() => { feedback.hidden = true; }, 3000);
  }
  document.querySelectorAll('form[data-cart-add]').forEach(form => form.addEventListener('submit', async event => {
    event.preventDefault();
    const button = form.querySelector('button[type="submit"]');
    if (button.disabled) return;
    const original = button.innerHTML;
    button.disabled = true;
    try {
      const response = await fetch(form.action, {
        method: 'POST', body: new FormData(form), credentials: 'same-origin',
        headers: { Accept: 'application/json' }
      });
      if (!response.headers.get('content-type')?.includes('application/json')) {
        if (response.redirected) window.location.href = response.url;
        else showFeedback('Không thêm được món đồ. Vui lòng thử lại.', true);
        return;
      }
      const result = await response.json();
      if (!response.ok) { showFeedback(result.error || 'Không thêm được món đồ.', true); return; }
      cartUpdated = true;
      showCount(result.count);
      showFeedback(result.message || 'Đã thêm vào giỏ hàng.');
      button.textContent = '✓ Đã thêm';
      setTimeout(() => { button.innerHTML = original; }, 1700);
    } catch { showFeedback('Không kết nối được. Vui lòng thử lại.', true); }
    finally { button.disabled = false; }
  }));
})();

(() => {
  const badges = document.querySelectorAll('[data-unread-badge]');
  if (!badges.length) return;

  let inFlight = false;
  async function refreshUnreadCounts() {
    if (inFlight || document.visibilityState === 'hidden') return;
    inFlight = true;
    try {
      const response = await fetch('/api/v1/unread-counts', {
        credentials: 'same-origin', cache: 'no-store', headers: { Accept: 'application/json' }
      });
      if (!response.ok) return;
      const counts = await response.json();
      for (const badge of badges) {
        const type = badge.dataset.unreadBadge;
        const count = Math.max(0, Number(counts[type]) || 0);
        badge.hidden = count === 0;
        badge.textContent = count > 99 ? '99+' : String(count);
        const link = badge.closest('[data-unread-label]');
        if (link) link.setAttribute('aria-label', count > 0
          ? `${link.dataset.unreadLabel}: ${count} chưa đọc`
          : link.dataset.unreadLabel);
      }
    } catch {
      // Keep the last displayed counts until the next refresh.
    } finally {
      inFlight = false;
    }
  }

  window.addEventListener('coco:unread-changed', refreshUnreadCounts);
  document.addEventListener('visibilitychange', () => {
    if (document.visibilityState === 'visible') refreshUnreadCounts();
  });
  window.addEventListener('focus', refreshUnreadCounts);
  window.setInterval(refreshUnreadCounts, 15000);
  refreshUnreadCounts();

  if (!window.signalR) return;
  const connection = new signalR.HubConnectionBuilder()
    .withUrl('/hubs/conversations')
    .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
    .build();
  connection.on('UnreadCountsChanged', refreshUnreadCounts);
  connection.onreconnected(refreshUnreadCounts);
  connection.onclose(() => window.setTimeout(connect, 5000));
  async function connect() {
    if (connection.state !== signalR.HubConnectionState.Disconnected) return;
    try { await connection.start(); }
    catch { window.setTimeout(connect, 5000); }
  }
  connect();
})();
