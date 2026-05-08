import { useState } from 'react'
import { api } from '../api/client'

interface Props {
  onClose: () => void
  onCreated: (key: string) => void
}

export default function CreateKeyModal({ onClose, onCreated }: Props) {
  const [name, setName] = useState('')
  const [projectName, setProjectName] = useState('')
  const [dailyLimitUsd, setDailyLimitUsd] = useState('10')
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [createdKey, setCreatedKey] = useState<string | null>(null)

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError(null)
    setSaving(true)
    try {
      const result = await api.createKey({
        name,
        projectName,
        dailyLimitUsd: parseFloat(dailyLimitUsd),
      })
      setCreatedKey(result.key)
    } catch (err) {
      setError(String(err))
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50">
      <div className="bg-white rounded-2xl shadow-xl p-6 w-full max-w-md">
        {createdKey ? (
          <div className="space-y-4">
            <h2 className="text-lg font-semibold text-gray-900">Key Created ✓</h2>
            <p className="text-sm text-gray-600">
              Copy your API key — it won't be shown again.
            </p>
            <div className="bg-gray-100 rounded-lg p-3 font-mono text-sm break-all select-all">
              {createdKey}
            </div>
            <button
              onClick={() => onCreated(createdKey)}
              className="w-full bg-indigo-600 text-white py-2 rounded-lg text-sm font-medium hover:bg-indigo-700 transition"
            >
              Done
            </button>
          </div>
        ) : (
          <form onSubmit={handleSubmit} className="space-y-4">
            <h2 className="text-lg font-semibold text-gray-900">Create API Key</h2>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Name</label>
              <input
                required
                className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500"
                value={name}
                onChange={(e) => setName(e.target.value)}
                placeholder="My Service Key"
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Project</label>
              <input
                required
                className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500"
                value={projectName}
                onChange={(e) => setProjectName(e.target.value)}
                placeholder="my-project"
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Daily Limit (USD)
              </label>
              <input
                required
                type="number"
                min="0.01"
                step="0.01"
                className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500"
                value={dailyLimitUsd}
                onChange={(e) => setDailyLimitUsd(e.target.value)}
              />
            </div>

            {error && <p className="text-sm text-red-500">{error}</p>}

            <div className="flex gap-3 pt-2">
              <button
                type="button"
                onClick={onClose}
                className="flex-1 border border-gray-300 text-gray-700 py-2 rounded-lg text-sm font-medium hover:bg-gray-50 transition"
              >
                Cancel
              </button>
              <button
                type="submit"
                disabled={saving}
                className="flex-1 bg-indigo-600 text-white py-2 rounded-lg text-sm font-medium hover:bg-indigo-700 transition disabled:opacity-50"
              >
                {saving ? 'Creating…' : 'Create'}
              </button>
            </div>
          </form>
        )}
      </div>
    </div>
  )
}
