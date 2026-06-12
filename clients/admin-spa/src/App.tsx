import { useAuth } from 'react-oidc-context';
import Dashboard from './pages/Dashboard';

// The AuthProvider in main.tsx handles the OIDC callback automatically.
// App just needs to react to the auth state.
export default function App() {
  const auth = useAuth();

  if (auth.isLoading) {
    return <div style={{ padding: '2rem' }}>Loading...</div>;
  }

  if (auth.error) {
    return (
      <div style={{ padding: '2rem', color: 'red' }}>
        Authentication error: {auth.error.message}
      </div>
    );
  }

  if (!auth.isAuthenticated) {
    return (
      <div style={{ padding: '2rem', fontFamily: 'system-ui, sans-serif' }}>
        <h1 style={{ color: '#1a1a2e' }}>RAI Amsterdam — Admin</h1>
        <p>Central identity and integration platform.</p>
        <button
          onClick={() => auth.signinRedirect()}
          style={{ padding: '0.6rem 1.5rem', background: '#0066cc', color: 'white', border: 'none', borderRadius: '4px', cursor: 'pointer', fontSize: '1rem' }}
        >
          Sign in via IdP
        </button>
      </div>
    );
  }

  return <Dashboard />;
}
