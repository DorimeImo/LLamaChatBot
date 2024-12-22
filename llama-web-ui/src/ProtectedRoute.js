import React from 'react';
import { Navigate } from 'react-router-dom';
import useAuth from './hooks/useAuth';

function ProtectedRoute({ children }) {
    const { isAuthenticated, loading } = useAuth();

    console.log('ProtectedRoute - isAuthenticated:', isAuthenticated, 'Loading:', loading);

    if (loading) {
        return <div>Loading...</div>;
    }

    if (!isAuthenticated) {
        console.log('ProtectedRoute navigating to login.');
        return <Navigate to="/login" replace />;
    }

    return children;
}

export default ProtectedRoute;