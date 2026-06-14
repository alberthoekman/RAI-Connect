import { useEffect, useState, type SyntheticEvent } from 'react';
import { useAuth } from 'react-oidc-context';
import { Alert, Button, Form, Row, Col, Table } from 'react-bootstrap';
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

  const handleCreate = async (e: SyntheticEvent) => {
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

  const handleAssignRole = async (e: SyntheticEvent) => {
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

  return (
    <div>
      <h2 className="h5 mb-3">Users</h2>
      {error && <Alert variant="danger" dismissible onClose={() => setError('')}>{error}</Alert>}
      {message && <Alert variant="success" dismissible onClose={() => setMessage('')}>{message}</Alert>}

      <Table striped bordered hover className="mb-4">
        <thead>
          <tr>
            <th>Email</th>
            <th>Name</th>
            <th>Roles</th>
            <th />
          </tr>
        </thead>
        <tbody>
          {users.map(u => (
            <tr key={u.id}>
              <td>{u.email}</td>
              <td>{u.displayName}</td>
              <td>{u.roles?.join(', ') || '—'}</td>
              <td>
                <Button variant="danger" size="sm" onClick={() => handleDelete(u.id)}>Delete</Button>
              </td>
            </tr>
          ))}
        </tbody>
      </Table>

      <h3 className="h6 mb-2">Create user</h3>
      <Form onSubmit={handleCreate} className="mb-4">
        <Row className="g-2 align-items-end">
          <Col xs="auto">
            <Form.Control placeholder="Email" value={email} onChange={e => setEmail(e.target.value)} required />
          </Col>
          <Col xs="auto">
            <Form.Control placeholder="Display name" value={displayName} onChange={e => setDisplayName(e.target.value)} />
          </Col>
          <Col xs="auto">
            <Form.Control placeholder="Password" type="password" value={password} onChange={e => setPassword(e.target.value)} required />
          </Col>
          <Col xs="auto">
            <Button type="submit" variant="primary">Create</Button>
          </Col>
        </Row>
      </Form>

      <h3 className="h6 mb-2">Assign role</h3>
      <Form onSubmit={handleAssignRole} className="mb-4">
        <Row className="g-2 align-items-end">
          <Col xs="auto">
            <Form.Select value={selectedUser} onChange={e => setSelectedUser(e.target.value)} required>
              <option value="">Select user...</option>
              {users.map(u => <option key={u.id} value={u.id}>{u.email}</option>)}
            </Form.Select>
          </Col>
          <Col xs="auto">
            <Form.Control placeholder="Role name (e.g. Admin)" value={roleName} onChange={e => setRoleName(e.target.value)} required />
          </Col>
          <Col xs="auto">
            <Button type="submit" variant="primary">Assign</Button>
          </Col>
        </Row>
      </Form>

      <h3 className="h6 mb-1">Test protected endpoint (users:read)</h3>
      <p className="text-muted small mb-2">
        Grant or revoke the <code>users:read</code> permission on your role, then click below to confirm enforcement.
      </p>
      <Button variant="secondary" onClick={handleTestProtected}>GET /api/me/protected</Button>
    </div>
  );
}
