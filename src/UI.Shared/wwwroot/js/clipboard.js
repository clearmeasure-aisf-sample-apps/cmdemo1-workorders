// Work Order Manage (#276): Copy work order number to clipboard via Clipboard API.
export async function copyToClipboard(text) {
	if (!navigator.clipboard) {
		throw new Error('Clipboard API not available');
	}
	await navigator.clipboard.writeText(text);
}
