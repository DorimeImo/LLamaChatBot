import {React, useEffect} from 'react';
import { Navigate } from 'react-router-dom';
import useAuth from './hooks/useAuth';

function ProtectedRoute({ children }) {
    const { isAuthenticated, loading, refreshOrVerifyToken } = useAuth();

    console.log('ProtectedRoute - isAuthenticated:', isAuthenticated, 'Loading:', loading);
    
    useEffect(() => {
        if (loading) {
            console.log('Triggering refreshOrVerifyToken from useEffect.');
            refreshOrVerifyToken();
        }
    }, [loading, refreshOrVerifyToken]);

    
    if (loading) {
        console.log('LOADING.');
        return <div>Loading...</div>;
    }

    if (!isAuthenticated) {
        console.log('ProtectedRoute navigating to login.');
        return <Navigate to="/login" replace />;
    }

    return children;
}

export default ProtectedRoute;