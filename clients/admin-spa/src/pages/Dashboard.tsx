import { useState } from 'react';
import { useAuth } from 'react-oidc-context';
import { Container, Nav, Table, Button } from 'react-bootstrap';
import UsersPage from './Users';
import RolesPage from './Roles';

type Tab = 'overview' | 'users' | 'roles';

export default function Dashboard() {
  const auth = useAuth();
  const [tab, setTab] = useState<Tab>('overview');

  const user = auth.user;
  const profile = user?.profile;

  return (
    <Container className="py-4">
      <header className="d-flex justify-content-between align-items-center mb-4">
        <h1 className="mb-0">RAI Amsterdam &mdash; Admin</h1>
        <div className="text-end">
          <div className="fw-semibold">{profile?.name ?? profile?.email}</div>
          <Button
            variant="link"
            size="sm"
            className="p-0 text-muted"
            onClick={() => auth.signoutRedirect()}
          >
            Sign out
          </Button>
        </div>
      </header>

      <Nav variant="tabs" className="mb-4" activeKey={tab} onSelect={k => setTab((k ?? 'overview') as Tab)}>
        <Nav.Item><Nav.Link eventKey="overview">Overview</Nav.Link></Nav.Item>
        <Nav.Item><Nav.Link eventKey="users">Users</Nav.Link></Nav.Item>
        <Nav.Item><Nav.Link eventKey="roles">Roles</Nav.Link></Nav.Item>
      </Nav>

      {tab === 'overview' && (
        <div>
          <h2 className="h5 mb-3">Current session</h2>
          <Table bordered>
            <tbody>
              <tr><th style={{ width: 140 }}>Email</th><td>{profile?.email}</td></tr>
              <tr><th>Name</th><td>{profile?.name}</td></tr>
              <tr><th>Roles</th><td>{(profile as any)?.roles?.join(', ') || '—'}</td></tr>
              <tr><th>Permissions</th><td>{(profile as any)?.permissions?.join(', ') || '—'}</td></tr>
              <tr><th>Token expires</th><td>{user?.expires_at ? new Date(user.expires_at * 1000).toLocaleString() : '—'}</td></tr>
            </tbody>
          </Table>
        </div>
      )}

      {tab === 'users' && <UsersPage />}
      {tab === 'roles' && <RolesPage />}
    </Container>
  );
}
