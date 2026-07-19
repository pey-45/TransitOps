import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import { AuthProvider } from './auth/AuthContext'
import { ProtectedRoute } from './routes/ProtectedRoute'
import { AppLayout } from './components/AppLayout'
import { HomePage } from './pages/HomePage'
import { LoginPage } from './pages/LoginPage'
import { VehicleDetailPage, VehicleFormPage, VehicleListPage } from './pages/vehicles/VehiclePages'
import { DriverDetailPage, DriverFormPage, DriverListPage } from './pages/drivers/DriverPages'
import { CustomerDetailPage, CustomerFormPage, CustomerListPage } from './pages/customers/CustomerPages'

export function AppRoutes() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route element={<ProtectedRoute />}>
        <Route element={<AppLayout />}>
          <Route path="/" element={<HomePage />} />
          <Route path="/vehiculos" element={<VehicleListPage />} />
          <Route path="/vehiculos/nuevo" element={<VehicleFormPage />} />
          <Route path="/vehiculos/:id" element={<VehicleDetailPage />} />
          <Route path="/vehiculos/:id/editar" element={<VehicleFormPage />} />
          <Route path="/conductores" element={<DriverListPage />} />
          <Route path="/conductores/nuevo" element={<DriverFormPage />} />
          <Route path="/conductores/:id" element={<DriverDetailPage />} />
          <Route path="/conductores/:id/editar" element={<DriverFormPage />} />
          <Route path="/clientes" element={<CustomerListPage />} />
          <Route path="/clientes/nuevo" element={<CustomerFormPage />} />
          <Route path="/clientes/:id" element={<CustomerDetailPage />} />
          <Route path="/clientes/:id/editar" element={<CustomerFormPage />} />
        </Route>
      </Route>
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}

export default function App() {
  return (
    <BrowserRouter>
      <AuthProvider><AppRoutes /></AuthProvider>
    </BrowserRouter>
  )
}
