import { useEffect, useState } from 'react';
import { useAuth } from 'react-oidc-context';
import type { UserDto } from '../api';
import {
  getUsers,
  createUser,
  deleteUser,
  assignRole,
  getProtected,
} from '../api';

export default function UsersPage() {
  const auth = useAuth();
  const token = auth.user?.access_token ?? '';

  const [users, setUsers] = useState<UserDto[]>([]);
  const [error, setError] = useState('');
  const [message, setMessage] = useState('');

  // New user form
  const [email, setEmail] = useState('');
  const [displayName, setDisplayName] = useState('');
  const [password, setPassword] = useState('');

  // Role assignment
  const [selectedUser, setSelectedUser] = useState('');
  const [roleName, setRoleName] = useState('');

  const load = async () => {
    try {
      setUsers(await getUsers(token));
    } catch (e: any) {
      setError(e.message);
    }
  };

  useEffect(() => { load(); }, []);

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      await createUser({ email, displayName, password }, token);
      setEmail(''); setDisplayName(''); setPassword('');
      setMessage('User created.');
      await load();
    } catch (e: any) { setError(e.message); }
  };

  const handleDelete = async (id: string) => {
    if (!confirm('Delete this user?')) return;
    try {
      await deleteUser(id, token);
      setMessage('User deleted.');
      await load();
    } catch (e: any) { setError(e.message); }
  };

  const handleAssignRole = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      await assignRole(selectedUser, roleName, token);
      setMessage(`Role "${roleName}" assigned.`);
      await load();
    } catch (e: any) { setError(e.message); }
  };

  const handleTestProtected = async () => {
    try {
      const res = await getProtected(token);
      setMessage(res.message);
    } catch (e: any) { setError(`Protected endpoint: ${e.message}`); }
  };

  const inputStyle: React.CSSProperties = { padding: '0.4rem', border: '1px solid #ccc', borderRadius: '4px', marginRight: '0.5rem' };
  const btnStyle: React.CSSProperties = { padding: '0.4rem 0.8rem', background: '#0066cc', color: 'white', border: 'none', borderRadius: '4px', cursor: 'pointer' };

  return (
    <div>
      <h2>Users</h2>
      {error && <p style={{ color: 'red' }}>{error}</p>}
      {message && <p style={{ color: 'green' }}>{message}</p>}

      <table style={{ width: '100%', borderCollapse: 'collapse', marginBottom: '1.5rem' }}>
        <thead>
          <tr style={{ borderBottom: '2px solid #ddd' }}>
            <th style={{ textAlign: 'left', padding: '0.4rem' }}>Email</th>
            <th style={{ textAlign: 'left', padding: '0.4rem' }}>Name</th>
            <th style={{ textAlign: 'left', padding: '0.4rem' }}>Roles</th>
            <th />
          </tr>
        </thead>
        <tbody>
          {users.map(u => (
            <tr key={u.id} style={{ borderBottom: '1px solid #eee' }}>
              <td style={{ padding: '0.4rem' }}>{u.email}</td>
              <td style={{ padding: '0.4rem' }}>{u.displayName}</td>
              <td style={{ padding: '0.4rem' }}>{u.roles?.join(', ') || '—'}</td>
              <td style={{ padding: '0.4rem' }}>
                <button onClick={() => handleDelete(u.id)} style={{ ...btnStyle, background: '#dc3545' }}>Delete</button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>

      <h3>Create user</h3>
      <form onSubmit={handleCreate} style={{ marginBottom: '1.5rem' }}>
        <input style={inputStyle} placeholder="Email" value={email} onChange={e => setEmail(e.target.value)} required />
        <input style={inputStyle} placeholder="Display name" value={displayName} onChange={e => setDisplayName(e.target.value)} />
        <input style={inputStyle} placeholder="Password" type="password" value={password} onChange={e => setPassword(e.target.value)} required />
        <button type="submit" style={btnStyle}>Create</button>
      </form>

      <h3>Assign role</h3>
      <form onSubmit={handleAssignRole} style={{ marginBottom: '1.5rem' }}>
        <select style={inputStyle} value={selectedUser} onChange={e => setSelectedUser(e.target.value)} required>
          <option value="">Select user...</option>
          {users.map(u => <option key={u.id} value={u.id}>{u.email}</option>)}
        </select>
        <input style={inputStyle} placeholder="Role name (e.g. Admin)" value={roleName} onChange={e => setRoleName(e.target.value)} required />
        <button type="submit" style={btnStyle}>Assign</button>
      </form>

      <h3>Test protected endpoint (users:read)</h3>
      <p style={{ fontSize: '0.875rem', color: '#666' }}>
        Grant or revoke the <code>users:read</code> permission on your role, then click below to confirm enforcement.
      </p>
      <button onClick={handleTestProtected} style={btnStyle}>GET /api/me/protected</button>
    </div>
  );
}
