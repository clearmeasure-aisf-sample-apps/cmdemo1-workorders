// Work Order Search (#251): CSV download of the filtered results and the header checkbox's indeterminate state.
export function downloadCsv(fileName, text) {
	const blob = new Blob([text], { type: 'text/csv;charset=utf-8' });
	const url = URL.createObjectURL(blob);
	const link = document.createElement('a');
	link.href = url;
	link.download = fileName;
	document.body.appendChild(link);
	link.click();
	link.remove();
	setTimeout(() => URL.revokeObjectURL(url), 1000);
}

export function setIndeterminate(element, value) {
	if (element) {
		element.indeterminate = value;
	}
}

export function focusById(id) {
	const element = document.getElementById(id);
	if (element) {
		element.focus();
	}
}
