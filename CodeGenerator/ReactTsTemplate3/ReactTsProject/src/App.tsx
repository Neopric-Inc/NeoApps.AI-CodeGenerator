import "styles/app.scss";
import "styles/global.scss";
import { Provider } from "react-redux";
import store from "redux/store";
import Pages from "pages";

function App(): JSX.Element {
  return (
    <Provider store={store}>
      <div className="App">
        <Pages />
      </div>
    </Provider >
  );
}

export default App;
