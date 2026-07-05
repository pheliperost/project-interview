export function openDatePicker(
  event: React.MouseEvent<HTMLInputElement> | React.FocusEvent<HTMLInputElement>,
) {
  const input = event.currentTarget;
  if (typeof input.showPicker !== 'function') return;
  try {
    input.showPicker();
  } catch {
    // Browser may reject if not a direct user gesture.
  }
}
