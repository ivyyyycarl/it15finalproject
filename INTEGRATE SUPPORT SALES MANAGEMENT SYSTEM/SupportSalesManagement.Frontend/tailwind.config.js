/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    "./**/*.{html,razor,cshtml}",
    "./wwwroot/**/*.{html,razor,cshtml}",
    "./tailwind-safelist.html"
  ],
  theme: {
    extend: {
      colors: {
        classic: {
            blue: '#80A1BA',  // Steel Blue
            teal: '#91C4C3',  // Soft Teal
            mint: '#B4DEBD',  // Mint Green
            cream: '#FFF7DD', // Warm Cream
            dark: '#2C3E50',  // Dark Text
        }
      }
    },
  },
  plugins: [],
}
