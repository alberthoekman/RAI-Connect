import { useEffect, useState, type SyntheticEvent } from 'react';
import { useAuth } from 'react-oidc-context';
import { Alert, Button, Card, Form, Row, Col } from 'react-bootstrap';
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

  const handleCreateRole = async (e: SyntheticEvent) => {
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
        setMessage(`Revoked "${perm.name}" from "${role.name}". Token refreshed — changes take effect immediately.`);
      } else {
        await grantPermission(role.id, perm.id, token);
        setMessage(`Granted "${perm.name}" to "${role.name}". Token refreshed — changes take effect immediately.`);
      }
      await load();
    } catch (e: any) { setError(e.message); return; }
    // Refresh the access token so the new permission claims are present immediately.
    auth.signinSilent().catch(() => {});
  };

  return (
    <div>
      <h2 className="h5 mb-3">Roles &amp; Permissions</h2>
      {error && <Alert variant="danger" dismissible onClose={() => setError('')}>{error}</Alert>}
      {message && <Alert variant="success" dismissible onClose={() => setMessage('')}>{message}</Alert>}

      <p className="text-muted small mb-3">
        Toggle a permission on a role to demonstrate live RBAC enforcement.
        After toggling, re-login (or wait for token refresh) and call the protected endpoint from the Users tab.
      </p>

      {roles.map(role => (
        <Card key={role.id} className="mb-3">
          <Card.Body>
            <Card.Title>{role.name}</Card.Title>
            <div className="d-flex flex-wrap gap-2">
              {permissions.map(perm => {
                const has = role.permissions.some(p => p.id === perm.id);
                return (
                  <Button
                    key={perm.id}
                    size="sm"
                    variant={has ? 'success' : 'outline-secondary'}
                    title={perm.description ?? perm.name}
                    onClick={() => togglePermission(role, perm)}
                  >
                    {perm.name}
                  </Button>
                );
              })}
            </div>
          </Card.Body>
        </Card>
      ))}

      <h3 className="h6 mb-2">Create role</h3>
      <Form onSubmit={handleCreateRole}>
        <Row className="g-2 align-items-end">
          <Col xs="auto">
            <Form.Control
              placeholder="Role name"
              value={newRoleName}
              onChange={e => setNewRoleName(e.target.value)}
              required
            />
          </Col>
          <Col xs="auto">
            <Button type="submit" variant="primary">Create</Button>
          </Col>
        </Row>
      </Form>
    </div>
  );
}
