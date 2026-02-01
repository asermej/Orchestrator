# Platform Web

The frontend application for the Surrova Platform, built with Next.js 15 and modern React patterns.

**[← Back to Main Documentation](../../README.md)**

## 🏗️ Architecture

The web application follows modern React patterns with a clean component structure:

```
┌─────────────────────────────────────────────────────────────┐
│                    Next.js App Router                      │
│              (Routing & Server Components)                 │
└─────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────────────────────────────────────┐
│                    React Components                        │
│              (UI Components & Business Logic)              │
└─────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────────────────────────────────────┐
│                    Tailwind CSS + Radix UI                │
│              (Styling & Component Library)                 │
└─────────────────────────────────────────────────────────────┘
                              │ HTTP API Calls
┌─────────────────────────────────────────────────────────────┐
│                    Platform.Api                            │
│              (.NET Backend)                                │
└─────────────────────────────────────────────────────────────┘
```

## 📁 Project Structure

```
apps/web/Platform.Web/
├── README.md                          # This file
├── package.json                       # Dependencies & Scripts
├── tsconfig.json                      # TypeScript Configuration
├── tailwind.config.ts                 # Tailwind CSS Configuration
├── next.config.mjs                    # Next.js Configuration
├── app/                               # App Router (Next.js 13+)
│   ├── layout.tsx                     # Root Layout
│   ├── page.tsx                       # Homepage
│   ├── globals.css                    # Global Styles
│   ├── loading.tsx                    # Loading UI
│   └── create-persona/                # Feature Pages
│       ├── page.tsx                   # Create Persona Page
│       └── actions.ts                 # Server Actions
├── components/                        # React Components
│   ├── theme-provider.tsx             # Theme Management
│   └── ui/                           # UI Component Library
│       ├── button.tsx                 # Button Component
│       ├── input.tsx                  # Input Component
│       ├── card.tsx                   # Card Component
│       ├── label.tsx                  # Label Component
│       ├── select.tsx                 # Select Component
│       └── textarea.tsx               # Textarea Component
├── hooks/                             # Custom React Hooks
│   ├── use-mobile.tsx                 # Mobile Detection Hook
│   └── use-toast.ts                   # Toast Notification Hook
├── lib/                               # Utility Libraries
│   └── utils.ts                       # Utility Functions
├── public/                            # Static Assets
│   ├── placeholder-logo.png           # Logo Assets
│   └── placeholder.svg                # SVG Assets
└── styles/                            # Additional Styles
    └── globals.css                    # Global CSS
```

## 🚀 Getting Started

### Prerequisites

From the main project directory:

```bash
# Install dependencies (from main README)
brew install node@18
```

### Local Development

1. **Navigate to web directory:**
   ```bash
   cd apps/web/Platform.Web
   ```

2. **Install dependencies:**
   ```bash
   npm install
   ```

3. **Start development server:**
   ```bash
   npm run dev
   ```

4. **Access the application:**
   - Open: `http://localhost:3000`

### Quick Commands

From the main project directory:

```bash
# Build everything (API + Web)
just build

# Start both API and Web with browser launch
just start
```

From the web directory (`apps/web/Platform.Web`):

```bash
# Development server
npm run dev

# Production build
npm run build

# Start production server
npm start

# Lint code
npm run lint
```

## 🎨 UI Components

### Component Library

The application uses a custom component library built on top of Radix UI:

- **Button** (`components/ui/button.tsx`) - Interactive button component
- **Input** (`components/ui/input.tsx`) - Form input fields
- **Card** (`components/ui/card.tsx`) - Content containers
- **Label** (`components/ui/label.tsx`) - Form labels
- **Select** (`components/ui/select.tsx`) - Dropdown selections
- **Textarea** (`components/ui/textarea.tsx`) - Multi-line text input

### Usage Example

```tsx
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'

export function ExampleForm() {
  return (
    <Card>
      <CardHeader>
        <CardTitle>Example Form</CardTitle>
      </CardHeader>
      <CardContent>
        <Input placeholder="Enter text..." />
        <Button>Submit</Button>
      </CardContent>
    </Card>
  )
}
```

## 🎯 Features

### Current Pages

- **Homepage** (`/`) - Main landing page
- **Create Persona** (`/create-persona`) - Persona creation form

### Styling

- **Tailwind CSS** - Utility-first CSS framework
- **CSS Variables** - Theme-aware color system
- **Dark Mode** - Automatic theme switching
- **Responsive Design** - Mobile-first approach

### TypeScript Configuration

The application uses TypeScript with path aliases for clean imports:

```json
{
  "compilerOptions": {
    "baseUrl": ".",
    "paths": {
      "@/*": ["./*"]
    }
  }
}
```

This allows imports like:
```tsx
import { Button } from '@/components/ui/button'
import { utils } from '@/lib/utils'
```

## 🔧 Configuration

### Next.js Configuration

Key configurations in `next.config.mjs`:

```javascript
const nextConfig = {
  eslint: {
    ignoreDuringBuilds: true,  // Skip ESLint during builds
  },
  typescript: {
    ignoreBuildErrors: true,   // Skip TypeScript errors during builds
  },
  images: {
    unoptimized: true,         // Disable image optimization
  },
}
```

### Tailwind Configuration

Custom theme configuration in `tailwind.config.ts`:

- **Colors**: Custom color palette with CSS variables
- **Typography**: Custom font families and sizes
- **Spacing**: Extended spacing scale
- **Animations**: Custom animations and transitions

## 🌐 API Integration

### Server Actions

The application uses Next.js Server Actions for API communication:

```tsx
// app/create-persona/actions.ts
'use server'

export async function createPersona(formData: FormData) {
  const response = await fetch('https://localhost:5001/api/personas', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({
      name: formData.get('name'),
      description: formData.get('description'),
    }),
  })
  
  return response.json()
}
```

### API Endpoints

The web application communicates with these API endpoints:

- **Base URL**: `https://localhost:5001/api`
- **Personas**: `/personas` - CRUD operations for personas
- **Swagger**: `/swagger` - API documentation

## 🎨 Theming

### Dark Mode Support

The application supports automatic dark mode switching:

```tsx
// components/theme-provider.tsx
import { ThemeProvider } from 'next-themes'

export function ThemeProvider({ children }) {
  return (
    <ThemeProvider
      attribute="class"
      defaultTheme="system"
      enableSystem
      disableTransitionOnChange
    >
      {children}
    </ThemeProvider>
  )
}
```

### CSS Variables

Theme-aware colors using CSS variables:

```css
:root {
  --background: 0 0% 100%;
  --foreground: 222.2 84% 4.9%;
  --primary: 222.2 47.4% 11.2%;
}

.dark {
  --background: 222.2 84% 4.9%;
  --foreground: 210 40% 98%;
  --primary: 210 40% 98%;
}
```

## 🧪 Development Guidelines

### Component Development

1. **Create reusable components** in `components/ui/`
2. **Use TypeScript** for type safety
3. **Follow Tailwind patterns** for styling
4. **Implement accessibility** features
5. **Add proper error handling**

### File Organization

- **Pages**: Use App Router in `app/` directory
- **Components**: Reusable UI in `components/`
- **Hooks**: Custom hooks in `hooks/`
- **Utils**: Helper functions in `lib/`
- **Types**: TypeScript types co-located with components

### Code Standards

- Use TypeScript for all new code
- Follow React best practices
- Implement proper error boundaries
- Use Server Actions for API calls
- Maintain responsive design patterns

## 🚦 CI/CD Integration

The web application is automatically built and tested in GitHub Actions:

1. ✅ Node.js 18 setup
2. ✅ npm dependency installation
3. ✅ TypeScript compilation
4. ✅ Next.js build process
5. ✅ Static optimization

See: [GitHub Actions Configuration](../../.github/workflows/build.yml)

## 🔍 Available Scripts

```bash
# Development
npm run dev          # Start development server
npm run build        # Build for production
npm start           # Start production server
npm run lint        # Run ESLint

# From main directory
just build          # Build both API and Web
just start          # Start both applications
```

## 📱 Responsive Design

The application is built mobile-first with responsive breakpoints:

- **Mobile**: `< 768px`
- **Tablet**: `768px - 1024px`
- **Desktop**: `> 1024px`

### Mobile Hook

```tsx
// hooks/use-mobile.tsx
import { useEffect, useState } from 'react'

export function useMobile() {
  const [isMobile, setIsMobile] = useState(false)
  
  useEffect(() => {
    const checkMobile = () => setIsMobile(window.innerWidth < 768)
    checkMobile()
    window.addEventListener('resize', checkMobile)
    return () => window.removeEventListener('resize', checkMobile)
  }, [])
  
  return isMobile
}
```

## 🛠️ Adding New Features

### Creating a New Page

1. **Create page file**: `app/new-feature/page.tsx`
2. **Add server actions**: `app/new-feature/actions.ts` (if needed)
3. **Create components**: `components/new-feature/`
4. **Add types**: Co-locate with components
5. **Update navigation**: Add links in layout or components

### Adding New Components

1. **Create component**: `components/ui/new-component.tsx`
2. **Export from index**: Update component exports
3. **Add to Storybook**: Document component usage
4. **Write tests**: Add component tests

---

**Navigation:**
- [← Main Documentation](../../README.md)
- [API Documentation →](../api/README.md)
- [GitHub Actions →](../../.github/workflows/build.yml) 