import { useEffect, useState } from 'react'
import { IconLookup, IconName, library } from '@fortawesome/fontawesome-svg-core'

export const useFontAwesomeIconPack = () => {
  const [iconPack, setIconPack] = useState<IconLookup[]>()

  useEffect(() => {
    if (!iconPack) {
      import('@fortawesome/free-solid-svg-icons').then((module) => {
        //Delete problematic icons
        const fas = { ...module.fas }
        delete fas.faCookie
        delete fas.faFontAwesomeLogoFull
        console.log(Object.keys(fas).length)

        const icons = Object.values(fas).map((icon) => ({
          prefix: icon.prefix,
          icon: icon.icon,
          iconName: icon.iconName as IconName
        }))
        library.add(...icons)
        setIconPack(icons)
      })
    }
  }, [iconPack])

  return iconPack
}
