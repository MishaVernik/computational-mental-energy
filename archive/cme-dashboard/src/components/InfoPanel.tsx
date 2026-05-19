import { useState } from 'react'
import { Info, ChevronDown, ChevronUp } from 'lucide-react'
import './InfoPanel.css'

interface Props {
  title: string
  children: React.ReactNode
  defaultOpen?: boolean
}

export default function InfoPanel({ title, children, defaultOpen = false }: Props) {
  const [isOpen, setIsOpen] = useState(defaultOpen)

  return (
    <div className="info-panel">
      <button
        className="info-panel-header"
        onClick={() => setIsOpen(!isOpen)}
        aria-expanded={isOpen}
      >
        <div className="info-panel-title">
          <Info size={18} />
          <span>{title}</span>
        </div>
        {isOpen ? <ChevronUp size={18} /> : <ChevronDown size={18} />}
      </button>
      
      {isOpen && (
        <div className="info-panel-content">
          {children}
        </div>
      )}
    </div>
  )
}


