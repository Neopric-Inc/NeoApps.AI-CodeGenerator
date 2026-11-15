// Copy buttons on every page.
    document.addEventListener('click', async (event) => {
      const button = event.target.closest('.copy-button');
      if (!button) return;
      const box = button.closest('.prompt-box, .copy-inline');
      const text = box?.querySelector('.copy-text')?.textContent.trim();
      const status = box?.querySelector('.copy-status');
      if (!text || !status) return;
      button.dataset.defaultLabel ||= button.textContent.trim();
      try {
        await navigator.clipboard.writeText(text);
        status.textContent = 'Copied';
        button.textContent = 'Copied';
        window.setTimeout(() => { status.textContent = ''; button.textContent = button.dataset.defaultLabel; }, 1800);
      } catch {
        status.textContent = 'Open the preview and select the prompt to copy.';
      }
    });

// Respect reduced motion: show the poster instead of playing demo videos
if (window.matchMedia('(prefers-reduced-motion: reduce)').matches) {
  document.querySelectorAll('video[autoplay]').forEach(v => { v.removeAttribute('autoplay'); v.pause(); v.controls = true; });
}
