import { useState } from 'react';
import { Link } from 'react-router-dom';
import { toast } from 'sonner';
import { api } from '@/api/client';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';

export function ForgotPasswordPage() {
  const [email, setEmail] = useState('');
  const [loading, setLoading] = useState(false);
  const [message, setMessage] = useState<string | null>(null);
  const [resetLink, setResetLink] = useState<string | null>(null);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setLoading(true);
    setMessage(null);
    setResetLink(null);
    try {
      const response = await api.forgotPassword(email);
      setMessage(response.message);
      setResetLink(response.resetLink ?? null);
      if (!response.resetLink) {
        toast.message('Check your email', { description: response.message });
      }
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Request failed');
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="auth-shell flex items-center justify-center p-4 sm:p-6">
      <div className="w-full max-w-md space-y-4">
        <form
          onSubmit={handleSubmit}
          className="space-y-4 rounded-2xl border border-border bg-card p-6 shadow-xl sm:p-8"
        >
          <div>
            <p className="text-primary text-sm font-semibold tracking-wide">Simple Tasks</p>
            <h1 className="mt-1 text-2xl font-bold">Forgot password</h1>
            <p className="mt-1 text-sm text-muted-foreground">
              Enter your email and we will send reset instructions if an account exists.
            </p>
          </div>
          <div className="space-y-2">
            <Label htmlFor="email">Email</Label>
            <Input
              id="email"
              type="email"
              required
              value={email}
              onChange={(e) => setEmail(e.target.value)}
            />
          </div>
          <Button type="submit" disabled={loading} className="w-full">
            {loading ? 'Sending…' : 'Send reset link'}
          </Button>
          <p className="text-center text-sm text-muted-foreground">
            <Link to="/login" className="text-primary hover:underline">
              Back to sign in
            </Link>
          </p>
        </form>

        {resetLink && (
          <div
            className="space-y-3 rounded-2xl border border-amber-500/40 bg-amber-500/10 p-5 shadow-sm"
            data-testid="demo-reset-card"
          >
            <p className="text-sm font-semibold text-amber-900 dark:text-amber-100">Demo only</p>
            <p className="text-sm text-muted-foreground">
              No real email is sent. Because this account exists, use the link below to reset your password:
            </p>
            {message && <p className="text-sm">{message}</p>}
            <Link
              to={resetLink.replace(/^https?:\/\/[^/]+/, '')}
              className="inline-flex h-9 w-full items-center justify-center rounded-lg bg-primary px-2.5 text-sm font-medium text-primary-foreground hover:bg-primary/80"
            >
              Open reset page
            </Link>
          </div>
        )}
      </div>
    </div>
  );
}
