"use client"

import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar'
import { cn } from '@/lib/utils'
import { getImageUrl } from '@/lib/config'

interface PersonaAvatarProps {
  imageUrl?: string | null
  displayName: string
  size?: 'sm' | 'md' | 'lg' | 'xl' | '2xl'
  shape?: 'circle' | 'square'
  className?: string
}

const sizeClasses = {
  sm: 'h-8 w-8 text-xs',
  md: 'h-10 w-10 text-sm',
  lg: 'h-16 w-16 text-lg',
  xl: 'h-24 w-24 text-2xl',
  '2xl': 'h-40 w-40 text-4xl'
}

export function PersonaAvatar({ 
  imageUrl, 
  displayName, 
  size = 'md',
  shape = 'circle',
  className 
}: PersonaAvatarProps) {
  // Generate initials from display name
  const getInitials = (name: string) => {
    const words = name.trim().split(/\s+/)
    if (words.length === 1) {
      return words[0].substring(0, 2).toUpperCase()
    }
    return (words[0][0] + words[words.length - 1][0]).toUpperCase()
  }

  const initials = getInitials(displayName)
  const fullImageUrl = getImageUrl(imageUrl)
  const shapeClass = shape === 'square' ? 'rounded-lg' : 'rounded-full'
  const borderRadius = shape === 'square' ? '0.5rem' : '9999px'

  return (
    <Avatar className={cn(sizeClasses[size], shapeClass, className)} style={{ borderRadius }}>
      {fullImageUrl && (
        <AvatarImage 
          src={fullImageUrl} 
          alt={displayName}
          className="object-cover"
          style={{ borderRadius }}
        />
      )}
      <AvatarFallback className={cn("bg-primary/10 text-primary font-semibold", shapeClass)} style={{ borderRadius }}>
        {initials}
      </AvatarFallback>
    </Avatar>
  )
}

