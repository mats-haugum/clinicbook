import { useState, useEffect } from 'react';
import { searchDoctors, type DoctorSearchResult } from '../api/doctors';
import { extractApiError } from '../utils/apiError';

export default function SearchPage() {
	const [name, setName] = useState('');
	const [results, setResults] = useState<DoctorSearchResult[]>([]);
	const [loading, setLoading] = useState(false);
	const [error, setError] = useState<string | null>(null);
	// Tracks whether the user has typed anything yet — so we don't show
	// "no results" before they've had a chance to type.
	const [hasSearched, setHasSearched] = useState(false);

	// useEffect runs a side effect whenever its dependencies change.
	// Here the dependency is `name` — so this runs on every keystroke.
	useEffect(() => {
		// If the input is cleared, reset everything and stop.
		if (!name.trim()) {
			setResults([]);
			setError(null);
			setHasSearched(false);
			return;
		}

		// Schedule the API call 400ms in the future.
		// If the user types again before 400ms, the cleanup below cancels this timer
		// and a new one is scheduled — so the API is only called when typing pauses.
		const timer = setTimeout(async () => {
			setLoading(true);
			setError(null);
			setHasSearched(true);

			try {
				const data = await searchDoctors(name);
				setResults(data);
			} catch (err: unknown) {
				const status = (err as { response?: { status: number } }).response?.status;
				if (status === 404) {
					setResults([]);
				} else {
					setError(extractApiError(err, 'Something went wrong. Please try again.'));
				}
			} finally {
				setLoading(false);
			}
		}, 400);

		// This cleanup function runs before the next effect fires.
		// It cancels the pending timer, preventing a stale API call.
		return () => clearTimeout(timer);
	}, [name]);

	return (
		<div className="max-w-3xl mx-auto px-6 py-12">
			<h1 className="text-3xl font-bold text-primary mb-2">Find a Doctor</h1>
			<p className="text-gray-500 mb-8">Search by first or last name.</p>
			<div className="relative mb-8">
				<input
					type="text"
					value={name}
					onChange={(e) => setName(e.target.value)}
					placeholder="e.g. James or Wilson"
					className="w-full border border-gray-300 rounded-lg px-4 py-3 pr-10 focus:outline-none focus:ring-2 focus:ring-accent"
				/>
				{/* Spinner inside the input while loading */}
				{loading && (
					<div className="absolute right-3 top-1/2 -translate-y-1/2">
						<div className="w-5 h-5 border-2 border-accent border-t-transparent rounded-full animate-spin" />
					</div>
				)}
			</div>

			{error && <p className="text-red-600 bg-red-50 border border-red-200 rounded-lg px-4 py-3 mb-4">{error}</p>}

			{results.length > 0 && (
				<ul className="space-y-3">
					{results.map((result, index) => (
						<li key={index} className="bg-white border border-gray-200 rounded-xl p-5 shadow-sm">
							<p className="text-lg font-semibold text-primary">{result.fullName}</p>
							<p className="text-sm text-gray-500 mt-1">
								{result.speciality} &middot; {result.clinicName}
							</p>
						</li>
					))}
				</ul>
			)}

			{/* Only show "no results" after a real search has completed with no matches */}
			{hasSearched && !loading && results.length === 0 && !error && (
				<p className="text-gray-500 text-center py-8">No doctors found matching "{name}".</p>
			)}
		</div>
	);
}
