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

// A text field is where the user is typing, so "/" must be left alone there.
function isTextField(target) {
	if (!target || typeof target.tagName !== 'string') {
		return false;
	}
	if (target.isContentEditable) {
		return true;
	}
	const tag = target.tagName;
	if (tag === 'TEXTAREA') {
		return true;
	}
	if (tag !== 'INPUT') {
		return false;
	}
	const type = (target.getAttribute('type') || 'text').toLowerCase();
	switch (type) {
		case 'button':
		case 'checkbox':
		case 'radio':
		case 'submit':
		case 'reset':
		case 'file':
		case 'image':
		case 'range':
		case 'color':
		case 'hidden':
			return false;
		default:
			return true;
	}
}

// Ctrl+K (Cmd+K on macOS) focuses the global search box in the top bar.
// "/" does the same when focus is not already in a text field.
export function initGlobalSearchShortcut(inputId) {
	const handler = (event) => {
		const input = document.getElementById(inputId);
		if (!input) {
			return;
		}

		const commandK = (event.ctrlKey || event.metaKey) && (event.key === 'k' || event.key === 'K');
		const slashOutsideText = event.key === '/'
			&& !event.ctrlKey
			&& !event.metaKey
			&& !event.altKey
			&& !isTextField(event.target);
		if (commandK || slashOutsideText) {
			event.preventDefault();
			input.focus();
			input.select();
		}
	};
	document.addEventListener('keydown', handler);
	return {
		dispose: () => document.removeEventListener('keydown', handler)
	};
}
