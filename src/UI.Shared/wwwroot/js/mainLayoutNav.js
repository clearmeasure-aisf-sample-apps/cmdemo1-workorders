// Breakpoint string must match MainLayout.NavRailBreakpointMediaQuery and @media in MainLayout.razor.css.
export function initNavToggle(dotNetRef, mediaQuery) {
	const mq = window.matchMedia(mediaQuery);
	const handler = () => {
		dotNetRef.invokeMethodAsync('OnViewportChanged', mq.matches);
	};
	mq.addEventListener('change', handler);
	handler();
	return {
		dispose: () => mq.removeEventListener('change', handler)
	};
}

// Ctrl+K (Cmd+K on macOS) focuses the global search box in the top bar.
export function initGlobalSearchShortcut(inputId) {
	const handler = (event) => {
		if ((event.ctrlKey || event.metaKey) && (event.key === 'k' || event.key === 'K')) {
			const input = document.getElementById(inputId);
			if (input) {
				event.preventDefault();
				input.focus();
				input.select();
			}
		}
	};
	document.addEventListener('keydown', handler);
	return {
		dispose: () => document.removeEventListener('keydown', handler)
	};
}
