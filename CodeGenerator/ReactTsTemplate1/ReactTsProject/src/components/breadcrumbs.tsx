import { Link, useLocation } from "react-router-dom";

function Breadcrumbs() {
  const location = useLocation();
  const pathnames = location.pathname.split("/").filter((x) => x);

  return (
    <nav>
      <ol>
        {pathnames.map((name, index) => {
          const routeTo = `/${pathnames.slice(0, index + 1).join("/")}`;
          const isLast = index === pathnames.length - 1;

          return isLast ? (
            <li key={name}>{name}</li>
          ) : (
            <li key={name}>
              <Link to={routeTo}>{name}</Link>
            </li>
          );
        })}
      </ol>
    </nav>
  );
}
