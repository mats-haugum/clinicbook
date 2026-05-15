import { useState } from 'react'
import { searchDoctors, type DoctorSearchResult } from '../api/doctors'

export default function SearchPage() {
  // useState stores values that the component needs to remember between renders.
  // Whenever any of these change, React re-renders the component automatically.
  const [name, setName]       = useState('')
  const [results, setResults] = useState<DoctorSearchResult[]>([])
  const [loading, setLoading] = useState(false)
  const [error, setError]     = useState<string | null>(null)

  async function handleSearch() {
    setLoading(true)
    setError(null)
    setResults([])

    try {
      const data = await searchDoctors(name)
      setResults(data)
    } catch (err: unknown) {
      // axios puts the HTTP status code on err.response.status
      const status = (err as { response?: { status: number } }).response?.status
      if (status === 404) {
        setError(`No doctors found matching "${name}".`)
      } else if (status === 400) {
        setError('Please enter a name to search.')
      } else {
        setError('Something went wrong. Please try again.')
      }
    } finally {
      // finally always runs — clears the spinner whether the call succeeded or failed
      setLoading(false)
    }
  }

  return (
    <div className="max-w-3xl mx-auto px-6 py-12">
      <h1 className="text-3xl font-bold text-primary mb-2">Find a Doctor</h1>
      <p className="text-gray-500 mb-8">Search by first or last name.</p>

      {/* e.preventDefault() stops the browser reloading the page on submit */}
      <form onSubmit={e => { e.preventDefault(); handleSearch() }} className="flex gap-3 mb-8">
        <input
          type="text"
          value={name}
          onChange={e => setName(e.target.value)}
          placeholder="e.g. James or Wilson"
          className="flex-1 border border-gray-300 rounded-lg px-4 py-2 focus:outline-none focus:ring-2 focus:ring-accent"
        />
        <button
          type="submit"
          disabled={loading}
          className="bg-accent text-white px-6 py-2 rounded-lg font-medium hover:bg-primary transition-colors disabled:opacity-50"
        >
          {loading ? 'Searching...' : 'Search'}
        </button>
      </form>

      {/* Loading spinner — animate-spin is a built-in Tailwind animation */}
      {loading && (
        <div className="flex justify-center py-12">
          <div className="w-8 h-8 border-4 border-accent border-t-transparent rounded-full animate-spin" />
        </div>
      )}

      {/* Error message */}
      {error && (
        <p className="text-red-600 bg-red-50 border border-red-200 rounded-lg px-4 py-3">
          {error}
        </p>
      )}

      {/* Results list */}
      {results.length > 0 && (
        <ul className="space-y-3">
          {results.map((result, index) => (
            // key helps React track which items changed when the list updates
            <li key={index} className="bg-white border border-gray-200 rounded-xl p-5 shadow-sm">
              <p className="text-lg font-semibold text-primary">{result.fullName}</p>
              <p className="text-sm text-gray-500 mt-1">
                {result.speciality} &middot; {result.clinicName}
              </p>
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}
