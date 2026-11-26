import React, { useState } from "react";
import { Button, IconButton, ListItemText, Menu, MenuItem, Tooltip } from "@mui/material";
import TranslateIcon from "@mui/icons-material/Translate";
import { api } from "../api";
import { t, useT } from "./index";

/**
 * Language menu. On the sign-in page the choice is kept in this browser; once signed in it is also saved for the user
 * (PUT Access/me/language), so it follows them to other devices and the API writes their emails in it.
 */
export const LanguagePicker: React.FC<{ signedIn?: boolean; compact?: boolean }> = ({ signedIn, compact }) => {
  const { language, languages, setLanguage } = useT();
  const [anchor, setAnchor] = useState<HTMLElement | null>(null);
  if (languages.length < 2) return null;
  const currentName = languages.find((l) => l.code === language)?.name || language;
  const choose = async (code: string) => {
    setAnchor(null);
    if (signedIn) await api("PUT", "Access/me/language", { language: code }).catch(() => undefined);
    await setLanguage(code);
  };
  return (
    <>
      {compact
        ? <Tooltip title={t("Language")}><IconButton aria-label={t("Language")} onClick={(e) => setAnchor(e.currentTarget)}><TranslateIcon /></IconButton></Tooltip>
        : <Button size="small" color="inherit" startIcon={<TranslateIcon />} onClick={(e) => setAnchor(e.currentTarget)} aria-label={t("Language")}>{currentName}</Button>}
      <Menu anchorEl={anchor} open={!!anchor} onClose={() => setAnchor(null)} slotProps={{ paper: { sx: { maxHeight: 420 } } }}>
        {languages.map((l) => (
          <MenuItem key={l.code} selected={l.code === language} onClick={() => choose(l.code)} lang={l.code} dir={l.dir || "ltr"}>
            <ListItemText primary={l.name} secondary={l.name !== l.english ? l.english : undefined} />
          </MenuItem>
        ))}
      </Menu>
    </>
  );
};
