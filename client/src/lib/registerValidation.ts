export type RegisterFieldErrors = {
  email?: string[];
  password?: string[];
  form?: string[];
};

const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

export function validateRegisterForm(email: string, password: string): RegisterFieldErrors {
  const errors: RegisterFieldErrors = {};
  const trimmedEmail = email.trim();

  if (!trimmedEmail) {
    errors.email = ['Email is required.'];
  } else if (!EMAIL_PATTERN.test(trimmedEmail)) {
    errors.email = ['Enter a valid email address (e.g. user@example.com).'];
  }

  const passwordErrors: string[] = [];
  if (!password) {
    passwordErrors.push('Password is required.');
  } else {
    if (password.length < 8) passwordErrors.push('Password must be at least 8 characters.');
    if (!/[A-Z]/.test(password)) passwordErrors.push('Include at least one uppercase letter (A-Z).');
    if (!/[a-z]/.test(password)) passwordErrors.push('Include at least one lowercase letter (a-z).');
    if (!/[0-9]/.test(password)) passwordErrors.push('Include at least one number (0-9).');
    if (!/[^a-zA-Z0-9]/.test(password)) {
      passwordErrors.push('Include at least one special character (e.g. ! @ #).');
    }
  }

  if (passwordErrors.length > 0) {
    errors.password = passwordErrors;
  }

  return errors;
}

export function hasFieldErrors(errors: RegisterFieldErrors) {
  return Boolean(errors.email?.length || errors.password?.length || errors.form?.length);
}

/** Map API error text to field-specific messages when possible. */
export function mapRegisterApiError(message: string, status: number): RegisterFieldErrors {
  const lower = message.toLowerCase();

  if (status === 409 || lower.includes('already registered')) {
    return { email: ['This email is already registered. Sign in or use another email.'] };
  }

  if (lower.includes('email') && (lower.includes('valid') || lower.includes('required'))) {
    return { email: [message] };
  }

  if (
    lower.includes('password') ||
    lower.includes('uppercase') ||
    lower.includes('lowercase') ||
    lower.includes('digit') ||
    lower.includes('special') ||
    lower.includes('character')
  ) {
    return { password: message.split(/\.\s+/).filter(Boolean) };
  }

  return { form: [message] };
}
