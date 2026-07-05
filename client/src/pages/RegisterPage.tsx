import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { toast } from 'sonner';
import { ApiError } from '@/api/client';
import { useAuth } from '@/auth/AuthContext';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import {
  hasFieldErrors,
  mapRegisterApiError,
  validateRegisterForm,
  type RegisterFieldErrors,
} from '@/lib/registerValidation';

function FieldErrors({ messages }: { messages?: string[] }) {
  if (!messages?.length) return null;

  return (
    <ul className="space-y-1 text-sm text-destructive" role="alert">
      {messages.map((message) => (
        <li key={message}>{message}</li>
      ))}
    </ul>
  );
}

export function RegisterPage() {
  const { register } = useAuth();
  const navigate = useNavigate();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [loading, setLoading] = useState(false);
  const [fieldErrors, setFieldErrors] = useState<RegisterFieldErrors>({});

  function clearFieldError(field: keyof RegisterFieldErrors) {
    setFieldErrors((current) => {
      const next = { ...current };
      delete next[field];
      return next;
    });
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();

    const clientErrors = validateRegisterForm(email, password);
    if (hasFieldErrors(clientErrors)) {
      setFieldErrors(clientErrors);
      toast.error('Please fix the highlighted fields.');
      return;
    }

    setFieldErrors({});
    setLoading(true);
    try {
      await register(email.trim(), password);
      navigate('/');
    } catch (err) {
      if (err instanceof ApiError) {
        const apiErrors = mapRegisterApiError(err.message, err.status);
        setFieldErrors(apiErrors);
        toast.error(apiErrors.form?.[0] ?? 'Could not create account. Check the fields below.');
      } else {
        toast.error(err instanceof Error ? err.message : 'Registration failed');
      }
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="auth-shell flex items-center justify-center p-4 sm:p-6">
      <form
        onSubmit={handleSubmit}
        noValidate
        className="w-full max-w-md space-y-4 rounded-2xl border border-border bg-card p-6 shadow-xl sm:p-8"
      >
        <div>
          <p className="text-primary text-sm font-semibold tracking-wide">Simple Tasks</p>
          <h1 className="mt-1 text-2xl font-bold">Create account</h1>
        </div>

        <FieldErrors messages={fieldErrors.form} />

        <div className="space-y-2">
          <Label htmlFor="email">Email</Label>
          <Input
            id="email"
            type="email"
            autoComplete="email"
            aria-invalid={Boolean(fieldErrors.email?.length)}
            value={email}
            onChange={(e) => {
              setEmail(e.target.value);
              clearFieldError('email');
              clearFieldError('form');
            }}
          />
          <FieldErrors messages={fieldErrors.email} />
        </div>

        <div className="space-y-2">
          <Label htmlFor="password">Password</Label>
          <Input
            id="password"
            type="password"
            autoComplete="new-password"
            aria-invalid={Boolean(fieldErrors.password?.length)}
            value={password}
            onChange={(e) => {
              setPassword(e.target.value);
              clearFieldError('password');
              clearFieldError('form');
            }}
          />
          <p className="text-xs text-muted-foreground">
            At least 8 characters with uppercase, lowercase, number, and special character.
          </p>
          <FieldErrors messages={fieldErrors.password} />
        </div>

        <Button type="submit" disabled={loading} className="w-full">
          {loading ? 'Creating…' : 'Register'}
        </Button>
        <p className="text-center text-sm text-muted-foreground">
          Already have an account?{' '}
          <Link to="/login" className="text-primary hover:underline">
            Sign in
          </Link>
        </p>
      </form>
    </div>
  );
}
