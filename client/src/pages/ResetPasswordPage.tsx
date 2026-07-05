import { useState } from 'react';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import { toast } from 'sonner';
import { api } from '@/api/client';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';

export function ResetPasswordPage() {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const [email] = useState(() => searchParams.get('email') ?? '');
  const [token] = useState(() => searchParams.get('token') ?? '');
  const [password, setPassword] = useState('');
  const [loading, setLoading] = useState(false);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!email || !token) {
      toast.error('Invalid reset link. Request a new one from the forgot password page.');
      return;
    }
    setLoading(true);
    try {
      const response = await api.resetPassword(email, token, password);
      toast.success(response.message);
      navigate('/login');
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Reset failed');
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="auth-shell flex items-center justify-center p-4 sm:p-6">
      <form
        onSubmit={handleSubmit}
        className="w-full max-w-md space-y-4 rounded-2xl border border-border bg-card p-6 shadow-xl sm:p-8"
      >
        <div>
          <p className="text-primary text-sm font-semibold tracking-wide">Simple Tasks</p>
          <h1 className="mt-1 text-2xl font-bold">Reset password</h1>
          <p className="mt-1 text-sm text-muted-foreground">Choose a new password for {email || 'your account'}.</p>
        </div>
        <div className="space-y-2">
          <Label htmlFor="password">New password</Label>
          <Input
            id="password"
            type="password"
            required
            minLength={8}
            value={password}
            onChange={(e) => setPassword(e.target.value)}
          />
          <p className="text-xs text-muted-foreground">
            Min 8 chars, upper, lower, digit, special character
          </p>
        </div>
        <Button type="submit" disabled={loading || !email || !token} className="w-full">
          {loading ? 'Saving…' : 'Reset password'}
        </Button>
        <p className="text-center text-sm text-muted-foreground">
          <Link to="/forgot-password" className="text-primary hover:underline">
            Request a new link
          </Link>
        </p>
      </form>
    </div>
  );
}
