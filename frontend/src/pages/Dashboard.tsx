import { useEffect, useState } from 'react'
import {
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
} from 'recharts'
import { api, type DashboardData } from '../api/client'

export default function Dashboard() {
  const [data, setData] = useState<DashboardData | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    api.getDashboard()
      .then(setData)
      .catch((e: Error) => setError(e.message))
      .finally(() => setLoading(false))
  }, [])

  if (loading) return <p className="text-gray-500">Loading…</p>
  if (error) return <p className="text-red-500">Error: {error}</p>
  if (!data) return null

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold text-gray-900">Dashboard</h1>

      {/* Summary card */}
      <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
        <StatCard
          title="Total Spend Today"
          value={`$${data.totalSpendTodayUsd.toFixed(4)}`}
          color="indigo"
        />
        {data.perProvider.map((p) => (
          <StatCard
            key={p.provider}
            title={p.provider}
            value={`$${p.totalCost.toFixed(4)}`}
            subtitle={`${p.requestCount} requests`}
            color="emerald"
          />
        ))}
      </div>

      {/* Spend chart */}
      <div className="bg-white rounded-xl shadow p-6">
        <h2 className="text-lg font-semibold text-gray-800 mb-4">Spend — Last 7 Days</h2>
        <ResponsiveContainer width="100%" height={240}>
          <LineChart data={data.spendLast7Days}>
            <CartesianGrid strokeDasharray="3 3" />
            <XAxis dataKey="date" tick={{ fontSize: 12 }} />
            <YAxis tick={{ fontSize: 12 }} tickFormatter={(v: number) => `$${v.toFixed(4)}`} />
            <Tooltip formatter={(v: number) => [`$${v.toFixed(6)}`, 'Spend']} />
            <Line type="monotone" dataKey="spendUsd" stroke="#6366f1" strokeWidth={2} dot={false} />
          </LineChart>
        </ResponsiveContainer>
      </div>

      {/* Top consumers */}
      {data.topConsumers.length > 0 && (
        <div className="bg-white rounded-xl shadow p-6">
          <h2 className="text-lg font-semibold text-gray-800 mb-4">Top Consumers</h2>
          <table className="w-full text-sm">
            <thead>
              <tr className="text-left text-gray-500 border-b">
                <th className="pb-2">Key Name</th>
                <th className="pb-2">Requests</th>
                <th className="pb-2 text-right">Cost</th>
              </tr>
            </thead>
            <tbody>
              {data.topConsumers.map((c) => (
                <tr key={c.apiKeyId} className="border-b last:border-0">
                  <td className="py-2">{c.apiKeyName}</td>
                  <td className="py-2">{c.requestCount}</td>
                  <td className="py-2 text-right font-mono">${c.totalCost.toFixed(6)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}

function StatCard({
  title,
  value,
  subtitle,
  color,
}: {
  title: string
  value: string
  subtitle?: string
  color: 'indigo' | 'emerald'
}) {
  const bg = color === 'indigo' ? 'bg-indigo-50' : 'bg-emerald-50'
  const text = color === 'indigo' ? 'text-indigo-700' : 'text-emerald-700'
  return (
    <div className={`${bg} rounded-xl p-5 shadow-sm`}>
      <p className="text-sm text-gray-500">{title}</p>
      <p className={`text-2xl font-bold mt-1 ${text}`}>{value}</p>
      {subtitle && <p className="text-xs text-gray-400 mt-1">{subtitle}</p>}
    </div>
  )
}
