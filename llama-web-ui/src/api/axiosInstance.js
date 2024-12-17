import axios from 'axios';

const axiosInstance = axios.create({
    baseURL: '/api',
    withCredentials: true, // Include cookies in requests
});

export default axiosInstance;