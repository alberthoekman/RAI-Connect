import { useEffect, useState } from 'react';
import { useAuth } from 'react-oidc-context';
import type { RoleDto, PermissionDto } from '../api';
import {
  getRoles,
  getPermissions,
  createRole,
  grantPermission,
  revokePermission,
} from '../api';

export default function RolesPage() {
  const auth = useAuth();
  const token = auth.user?.access_token ?? '';

  const [roles, setRoles] = useState<RoleDto[]>([]);
  const [permissions, setPermissions] = useState<PermissionDto[]>([]);
  const [error, setError] = useState('');
  const [message, setMessage] = useState('');
  const [newRoleName, setNewRoleName] = useState('');

  const load = async () => {
    try {
      const [r, p] = await Promise.all([getRoles(token), getPermissions(token)]);
      setRoles(r);
      setPermissions(p);
    } catch (e: any) { setError(e.message); }
  };

  useEffect(() => { load(); }, []);

  const handleCreateRole = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      await createRole(newRoleName, token);
      setNewRoleName('');
      setMessage(`Role "${newRoleName}" created.`);
      await load();
    } catch (e: any) { setError(e.message); }
  };

  const togglePermission = async (role: RoleDto, perm: PermissionDto) => {
    const has = role.permissions.some(p => p.id === perm.id);
    try {
      if (has) {
        await revokePermission(role.id, perm.id, token);
        setMessage(`Revoked "${perm.name}" from "${role.name}".`);
      } else {
        await grantPermission(role.id, perm.id, token);
        setMessage(`Granted "${perm.name}" to "${role.name}".`);
      }
      await load();
    } catch (e: any) { setError(e.message); }
  };

  const inputStyle: React.CSSProperties = { padding: '0.4rem', border: '1px solid #ccc', borderRadius: '4px', marginRight: '0.5rem' };
  const btnStyle: React.CSSProperties = { padding: '0.4rem 0.8rem', background: '#0066cc', color: 'white', border: 'none', borderRadius: '4px', cursor: 'pointer' };

  return (
    <div>
      <h2>Roles &amp; Permissions</h2>
      {error && <p style={{ color: 'red' }}>{error}</p>}
      {message && <p style={{ color: 'green' }}>{message}</p>}

      <p style={{ fontSize: '0.875rem', color: '#555' }}>
        Toggle a permission on a role to demonstrate live RBAC enforcement.
        After toggling, re-login (or wait for token refresh) and call the protected endpoint from the Users tab.
      </p>

      {roles.map(role => (
        <div key={role.id} style={{ border: '1px solid #ddd', borderRadius: '6px', padding: '1rem', marginBottom: '1rem' }}>
          <h3 style={{ margin: '0 0 0.75rem' }}>{role.name}</h3>
          <div style={{ display: 'flex', flexWrap: 'wrap', gap: '0.5rem' }}>
            {permissions.map(perm => {
              const has = role.permissions.some(p => p.id === perm.id);
              return (
                <button
                  key={perm.id}
                  onClick={() => togglePermission(role, perm)}
                  title={perm.description ?? perm.name}
                  style={{
                    padding: '0.3rem 0.75rem',
                    border: '1px solid',
                    borderRadius: '12px',
                    cursor: 'pointer',
                    background: has ? '#d4edda' : '#f8f9fa',
                    borderColor: has ? '#28a745' : '#ccc',
                    color: has ? '#155724' : '#555',
                  }}
                >
                  {has ? '+ ' : ''}{perm.name}
                </button>
              );
            })}
          </div>
        </div>
      ))}

      <h3>Create role</h3>
      <form onSubmit={handleCreateRole}>
        <input style={inputStyle} placeholder="Role name" value={newRoleName} onChange={e => setNewRoleName(e.target.value)} required />
        <button type="submit" style={btnStyle}>Create</button>
      </form>
    </div>
  );
}
