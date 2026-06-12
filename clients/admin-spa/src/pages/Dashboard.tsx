import { useState } from 'react';
import { useAuth } from 'react-oidc-context';
import UsersPage from './Users';
import RolesPage from './Roles';

type Tab = 'overview' | 'users' | 'roles';

export default function Dashboard() {
  const auth = useAuth();
  const [tab, setTab] = useState<Tab>('overview');

  const user = auth.user;
  const profile = user?.profile;

  const navStyle: React.CSSProperties = {
    display: 'flex',
    gap: '0.5rem',
    marginBottom: '1.5rem',
  };
  const btnStyle = (active: boolean): React.CSSProperties => ({
    padding: '0.4rem 1rem',
    background: active ? '#0066cc' : '#e9ecef',
    color: active ? 'white' : '#333',
    border: 'none',
    borderRadius: '4px',
    cursor: 'pointer',
  });

  return (
    <div style={{ fontFamily: 'system-ui, sans-serif', maxWidth: 900, margin: '2rem auto', padding: '0 1rem' }}>
      <header style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1.5rem' }}>
        <h1 style={{ margin: 0, color: '#1a1a2e' }}>RAI Amsterdam — Admin</h1>
        <div style={{ textAlign: 'right' }}>
          <div style={{ fontWeight: 600 }}>{profile?.name ?? profile?.email}</div>
          <button
            onClick={() => auth.signoutRedirect()}
            style={{ marginTop: '0.25rem', fontSize: '0.85rem', background: 'none', border: 'none', cursor: 'pointer', color: '#666', textDecoration: 'underline' }}
          >
            Sign out
          </button>
        </div>
      </header>

      <nav style={navStyle}>
        <button style={btnStyle(tab === 'overview')} onClick={() => setTab('overview')}>Overview</button>
        <button style={btnStyle(tab === 'users')} onClick={() => setTab('users')}>Users</button>
        <button style={btnStyle(tab === 'roles')} onClick={() => setTab('roles')}>Roles</button>
      </nav>

      {tab === 'overview' && (
        <div>
          <h2>Current session</h2>
          <table style={{ borderCollapse: 'collapse', width: '100%' }}>
            <tbody>
              <tr><td style={{ padding: '0.4rem', fontWeight: 600, width: 140 }}>Email</td><td>{profile?.email}</td></tr>
              <tr><td style={{ padding: '0.4rem', fontWeight: 600 }}>Name</td><td>{profile?.name}</td></tr>
              <tr><td style={{ padding: '0.4rem', fontWeight: 600 }}>Roles</td><td>{(profile as any)?.roles?.join(', ') || '—'}</td></tr>
              <tr><td style={{ padding: '0.4rem', fontWeight: 600 }}>Permissions</td><td>{(profile as any)?.permission?.join(', ') || '—'}</td></tr>
              <tr><td style={{ padding: '0.4rem', fontWeight: 600 }}>Token expires</td><td>{user?.expires_at ? new Date(user.expires_at * 1000).toLocaleString() : '—'}</td></tr>
            </tbody>
          </table>
        </div>
      )}

      {tab === 'users' && <UsersPage />}
      {tab === 'roles' && <RolesPage />}
    </div>
  );
}
