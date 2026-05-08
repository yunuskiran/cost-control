import { BrowserRouter, Routes, Route, NavLink } from 'react-router-dom'
import Dashboard from './pages/Dashboard'
import ApiKeys from './pages/ApiKeys'

export default function App() {
  return (
    <BrowserRouter>
      <div className="min-h-screen bg-gray-50">
        <nav className="bg-white shadow-sm border-b border-gray-200">
          <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
            <div className="flex items-center h-16 gap-8">
              <span className="text-xl font-bold text-indigo-600">TokenGuard</span>
              <NavLink
                to="/"
                end
                className={({ isActive }) =>
                  `text-sm font-medium ${isActive ? 'text-indigo-600' : 'text-gray-500 hover:text-gray-700'}`
                }
              >
                Dashboard
              </NavLink>
              <NavLink
                to="/keys"
                className={({ isActive }) =>
                  `text-sm font-medium ${isActive ? 'text-indigo-600' : 'text-gray-500 hover:text-gray-700'}`
                }
              >
                API Keys
              </NavLink>
            </div>
          </div>
        </nav>
        <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
          <Routes>
            <Route path="/" element={<Dashboard />} />
            <Route path="/keys" element={<ApiKeys />} />
          </Routes>
        </main>
      </div>
    </BrowserRouter>
  )
}
