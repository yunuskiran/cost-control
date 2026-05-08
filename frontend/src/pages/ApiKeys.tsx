import { useEffect, useState } from 'react'
import { api, type ApiKeyRecord } from '../api/client'
import CreateKeyModal from '../components/CreateKeyModal'

export default function ApiKeys() {
  const [keys, setKeys] = useState<ApiKeyRecord[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [showModal, setShowModal] = useState(false)

  const load = () => {
    setLoading(true)
    api.listKeys()
      .then(setKeys)
      .catch((e: Error) => setError(e.message))
      .finally(() => setLoading(false))
  }

  useEffect(load, [])

  const handleDelete = async (id: string) => {
    if (!confirm('Deactivate this key?')) return
    try {
      await api.deleteKey(id)
      load()
    } catch (e) {
      alert(String(e))
    }
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-gray-900">API Keys</h1>
        <button
          onClick={() => setShowModal(true)}
          className="bg-indigo-600 text-white px-4 py-2 rounded-lg text-sm font-medium hover:bg-indigo-700 transition"
        >
          + New Key
        </button>
      </div>

      {loading && <p className="text-gray-500">Loading…</p>}
      {error && <p className="text-red-500">Error: {error}</p>}

      {!loading && !error && (
        <div className="bg-white rounded-xl shadow overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-gray-50 border-b">
              <tr className="text-left text-gray-500">
                <th className="px-4 py-3">Name</th>
                <th className="px-4 py-3">Project</th>
                <th className="px-4 py-3 text-right">Limit / day</th>
                <th className="px-4 py-3 text-right">Spent today</th>
                <th className="px-4 py-3 text-center">Status</th>
                <th className="px-4 py-3" />
              </tr>
            </thead>
            <tbody>
              {keys.map((k) => (
                <tr key={k.id} className="border-b last:border-0 hover:bg-gray-50">
                  <td className="px-4 py-3 font-medium text-gray-900">{k.name}</td>
                  <td className="px-4 py-3 text-gray-500">{k.projectName}</td>
                  <td className="px-4 py-3 text-right font-mono">${k.dailyLimitUsd.toFixed(2)}</td>
                  <td className="px-4 py-3 text-right font-mono">${k.spentTodayUsd.toFixed(4)}</td>
                  <td className="px-4 py-3 text-center">
                    <StatusBadge active={k.isActive} blocked={k.isBlocked} />
                  </td>
                  <td className="px-4 py-3 text-right">
                    <button
                      onClick={() => handleDelete(k.id)}
                      className="text-red-500 hover:text-red-700 text-xs font-medium"
                    >
                      Deactivate
                    </button>
                  </td>
                </tr>
              ))}
              {keys.length === 0 && (
                <tr>
                  <td colSpan={6} className="px-4 py-8 text-center text-gray-400">
                    No API keys yet. Create one to get started.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      )}

      {showModal && (
        <CreateKeyModal
          onClose={() => setShowModal(false)}
          onCreated={() => {
            setShowModal(false)
            load()
          }}
        />
      )}
    </div>
  )
}

function StatusBadge({ active, blocked }: { active: boolean; blocked: boolean }) {
  if (!active)
    return <span className="inline-block px-2 py-0.5 rounded-full text-xs bg-gray-100 text-gray-500">Inactive</span>
  if (blocked)
    return <span className="inline-block px-2 py-0.5 rounded-full text-xs bg-red-100 text-red-600">Blocked</span>
  return <span className="inline-block px-2 py-0.5 rounded-full text-xs bg-green-100 text-green-600">Active</span>
}
