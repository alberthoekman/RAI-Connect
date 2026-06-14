import { useAuth } from 'react-oidc-context';
import { Container, Spinner, Alert, Button } from 'react-bootstrap';
import Dashboard from './pages/Dashboard';

// The AuthProvider in main.tsx handles the OIDC callback automatically.
// App just needs to react to the auth state.
export default function App() {
  const auth = useAuth();

  if (auth.isLoading) {
    return (
      <Container className="pt-5 text-center">
        <Spinner animation="border" role="status" />
      </Container>
    );
  }

  if (auth.error) {
    return (
      <Container className="pt-5">
        <Alert variant="danger">Authentication error: {auth.error.message}</Alert>
      </Container>
    );
  }

  if (!auth.isAuthenticated) {
    return (
      <Container className="pt-5">
        <h1 className="mb-3">RAI Amsterdam &mdash; Admin</h1>
        <p className="mb-4">Central identity and integration platform.</p>
        <Button variant="primary" onClick={() => auth.signinRedirect()}>
          Sign in via IdP
        </Button>
      </Container>
    );
  }

  return <Dashboard />;
}
