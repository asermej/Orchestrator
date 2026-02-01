"use client"

import { useState, useCallback } from 'react'
import { useDropzone } from 'react-dropzone'
import imageCompression from 'browser-image-compression'
import { Button } from '@/components/ui/button'
import { Card } from '@/components/ui/card'
import { Upload, X, Image as ImageIcon } from 'lucide-react'

interface ImageUploadProps {
  value?: string
  onChange: (url: string) => void
  onRemove?: () => void
  maxSizeMB?: number
  maxWidthOrHeight?: number
  accept?: string
  disabled?: boolean
  uploadAction?: (formData: FormData) => Promise<string>
}

export function ImageUpload({
  value,
  onChange,
  onRemove,
  maxSizeMB = 5,
  maxWidthOrHeight = 800,
  accept = 'image/*',
  disabled = false,
  uploadAction
}: ImageUploadProps) {
  const [isUploading, setIsUploading] = useState(false)
  const [uploadProgress, setUploadProgress] = useState(0)
  const [error, setError] = useState<string | null>(null)
  
  // Convert relative URLs to full URLs for preview
  const getPreviewUrl = (url: string | undefined) => {
    if (!url) return null
    // If it's already a full URL, return it
    if (url.startsWith('http://') || url.startsWith('https://')) {
      return url
    }
    // If it's a relative URL, prepend the base server URL (not API URL, since static files are served from root)
    const baseUrl = process.env.NEXT_PUBLIC_API_URL?.replace('/api/v1', '') || 'http://localhost:5000'
    return `${baseUrl}${url}`
  }
  
  const [preview, setPreview] = useState<string | null>(getPreviewUrl(value))

  const onDrop = useCallback(async (acceptedFiles: File[]) => {
    if (acceptedFiles.length === 0) return
    
    const file = acceptedFiles[0]
    setError(null)
    setIsUploading(true)
    setUploadProgress(0)

    try {
      // Validate file type
      if (!file.type.startsWith('image/')) {
        throw new Error('Please upload an image file')
      }

      // Validate file size before compression
      if (file.size > maxSizeMB * 1024 * 1024) {
        setUploadProgress(10)
        
        // Compress image
        const options = {
          maxSizeMB: 1,
          maxWidthOrHeight: maxWidthOrHeight,
          useWebWorker: true,
          fileType: file.type,
          initialQuality: 0.8
        }

        const compressedFile = await imageCompression(file, options)
        setUploadProgress(50)

        // Upload compressed file
        await uploadFile(compressedFile)
      } else {
        // Upload original file if it's already small enough
        await uploadFile(file)
      }
    } catch (err) {
      console.error('Upload error:', err)
      setError(err instanceof Error ? err.message : 'Failed to upload image')
      setIsUploading(false)
      setUploadProgress(0)
    }
  }, [maxSizeMB, maxWidthOrHeight, onChange, uploadAction])

  const uploadFile = async (file: File) => {
    const formData = new FormData()
    formData.append('file', file)

    setUploadProgress(60)

    let relativeUrl: string
    
    if (uploadAction) {
      // Use the provided server action for authenticated uploads
      try {
        relativeUrl = await uploadAction(formData)
        setUploadProgress(80)
      } catch (error) {
        throw error
      }
    } else {
      // Fall back to direct API call (for backwards compatibility or public uploads)
      const apiUrl = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000/api/v1'
      const response = await fetch(`${apiUrl}/Image/upload`, {
        method: 'POST',
        body: formData,
      })

      setUploadProgress(80)

      if (!response.ok) {
        const errorData = await response.json().catch(() => ({ message: 'Upload failed' }))
        throw new Error(errorData.message || 'Failed to upload image')
      }

      const data = await response.json()
      relativeUrl = data.url
    }
    
    // For preview, use the base URL (static files are served from root, not /api/v1)
    const baseUrl = process.env.NEXT_PUBLIC_API_URL?.replace('/api/v1', '') || 'http://localhost:5000'
    const previewUrl = `${baseUrl}${relativeUrl}`
    
    setPreview(previewUrl)
    // But store only the relative URL in the form
    onChange(relativeUrl)
    setUploadProgress(100)
    setIsUploading(false)
  }

  const { getRootProps, getInputProps, isDragActive } = useDropzone({
    onDrop,
    accept: {
      'image/*': ['.jpg', '.jpeg', '.png', '.webp', '.gif']
    },
    maxFiles: 1,
    disabled: disabled || isUploading
  })

  const handleRemove = () => {
    setPreview(null)
    setError(null)
    if (onRemove) {
      onRemove()
    } else {
      onChange('')
    }
  }

  if (preview) {
    return (
      <Card className="relative overflow-hidden">
        <div className="aspect-square w-full max-w-[300px] relative">
          <img
            src={preview}
            alt="Preview"
            className="object-cover w-full h-full"
          />
          {!disabled && (
            <Button
              type="button"
              variant="destructive"
              size="icon"
              className="absolute top-2 right-2"
              onClick={handleRemove}
            >
              <X className="h-4 w-4" />
            </Button>
          )}
        </div>
      </Card>
    )
  }

  return (
    <div className="space-y-2">
      <Card
        {...getRootProps()}
        className={`
          border-2 border-dashed p-8 text-center cursor-pointer
          transition-colors duration-200
          ${isDragActive ? 'border-primary bg-primary/5' : 'border-muted-foreground/25'}
          ${disabled || isUploading ? 'opacity-50 cursor-not-allowed' : 'hover:border-primary'}
        `}
      >
        <input {...getInputProps()} disabled={disabled || isUploading} />
        
        <div className="flex flex-col items-center justify-center gap-2">
          {isUploading ? (
            <>
              <Upload className="h-10 w-10 text-muted-foreground animate-pulse" />
              <div className="text-sm font-medium">Uploading... {uploadProgress}%</div>
              <div className="w-full max-w-xs bg-secondary rounded-full h-2 overflow-hidden">
                <div 
                  className="bg-primary h-full transition-all duration-300"
                  style={{ width: `${uploadProgress}%` }}
                />
              </div>
            </>
          ) : (
            <>
              <ImageIcon className="h-10 w-10 text-muted-foreground" />
              <div className="text-sm font-medium">
                {isDragActive ? 'Drop image here' : 'Drag & drop an image, or click to select'}
              </div>
              <div className="text-xs text-muted-foreground">
                Supports JPG, PNG, WEBP, GIF (max {maxSizeMB}MB)
              </div>
            </>
          )}
        </div>
      </Card>

      {error && (
        <div className="text-sm text-destructive">
          {error}
        </div>
      )}
    </div>
  )
}

