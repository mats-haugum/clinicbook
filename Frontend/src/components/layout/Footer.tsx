export default function Footer() {
  // new Date().getFullYear() always returns the current year at runtime
  const year = new Date().getFullYear()

  return (
    <footer className="bg-primary text-white py-6 px-6 text-center text-sm">
      <p>&copy; {year} ClinicBook. All rights reserved.</p>
    </footer>
  )
}
